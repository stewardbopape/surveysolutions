﻿using System;
using System.Collections.Generic;
using System.Linq;
using Main.Core.Entities.SubEntities;
using Ninject.Selection;
using WB.Core.BoundedContexts.Headquarters.OwinSecurity;
using WB.Core.BoundedContexts.Headquarters.Views.Interviewer;
using WB.Core.BoundedContexts.Headquarters.Views.Supervisor;
using WB.Core.GenericSubdomains.Portable;
using WB.Infrastructure.Native.Threading;

namespace WB.Core.BoundedContexts.Headquarters.Views.User
{
    public class UserViewFactory : IUserViewFactory
    {
        private readonly object lockObject = new object();
        protected readonly IUserRepository UserRepository;

        public UserViewFactory(IUserRepository UserRepository)
        {
            this.UserRepository = UserRepository;
        }

        public UserView GetUser(UserViewInputModel input)
        {
            HqUser user;
            lock (lockObject)
            {
                var users = this.UserRepository.Users;
                if (input.PublicKey != null)
                    users = users.Where(x => x.Id == input.PublicKey);
                else if (!string.IsNullOrEmpty(input.UserName))
                    users = users.Where(x => x.UserName.ToLower() == input.UserName.ToLower());
                else if (!string.IsNullOrEmpty(input.UserEmail))
                    users = users.Where(x => x.Email.ToLower() == input.UserEmail.ToLower());
                else if (!string.IsNullOrEmpty(input.DeviceId))
                    users = users.Where(x => x.Profile.DeviceId == input.DeviceId);

                user = users.FirstOrDefault();
            }

            if (user == null) return null;

            UserLight supervisor = null;
            lock (lockObject)
            {
                var superVisorId = user.Profile?.SupervisorId;
                if (superVisorId != null)
                {
                    var supervisorUser = AsyncHelper.RunSync(() => this.UserRepository.FindByIdAsync(user.Profile.SupervisorId.Value));

                    supervisor = new UserLight(superVisorId.Value, supervisorUser?.UserName);
                }
            }

            HashSet<UserRoles> userRole;
            lock (lockObject)
            {
                userRole = new HashSet<UserRoles>(new[] { user.Roles.First().Role });
            }
            return new UserView
            {
                CreationDate = user.CreationDate,
                UserName = user.UserName,
                Email = user.Email,
                IsLockedBySupervisor = user.IsLockedBySupervisor,
                IsLockedByHQ = user.IsLockedByHeadquaters,
                PublicKey = user.Id,
                Supervisor = supervisor,
                PersonName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                IsArchived = user.IsArchived,
                Roles = userRole
            };
        }

        public UserListView GetUsersByRole(int pageIndex, int pageSize, string orderBy, string searchBy, bool archived, UserRoles role)
        {
            Func<IQueryable<HqUser>, IQueryable<InterviewersItem>> query =
                allUsers => ApplyFilter(allUsers, searchBy, role, archived)
                    .Select(x => new InterviewersItem
                    {
                        UserId = x.Id,
                        CreationDate = x.CreationDate,
                        Email = x.Email,
                        IsArchived = x.IsArchived,
                        IsLockedBySupervisor = x.IsLockedBySupervisor,
                        IsLockedByHQ = x.IsLockedByHeadquaters,
                        UserName = x.UserName,
                        SupervisorName = allUsers.FirstOrDefault(pr => pr.Id == x.Profile.SupervisorId).UserName,
                        DeviceId = x.Profile.DeviceId
                    });

            orderBy = string.IsNullOrWhiteSpace(orderBy) ? nameof(HqUser.UserName) : orderBy;

            var filteredUsers = query
                .PagedAndOrderedQuery(orderBy, pageIndex, pageSize)
                .Invoke(this.UserRepository.Users)
                .ToList();

            return new UserListView
            {
                Page = pageIndex,
                PageSize = pageSize,
                TotalCount = query.Invoke(this.UserRepository.Users).Count(),
                Items = filteredUsers.ToList()
            };
        }

        public UsersView GetInterviewers(int pageSize, string searchBy, Guid? supervisorId, bool archived = false)
        {
            Func<IQueryable<HqUser>, IQueryable<HqUser>> query = users =>
            {
                users = ApplyFilter(users, searchBy, UserRoles.Interviewer, archived)
                    .Where(user => !user.IsLockedBySupervisor && !user.IsLockedByHeadquaters);

                if (supervisorId.HasValue)
                    users = users.Where(user => user.Profile.SupervisorId == supervisorId);

                return users;
            };

            var filteredUsers = query
                .PagedAndOrderedQuery(nameof(HqUser.UserName), 1, pageSize)
                .Invoke(this.UserRepository.Users)
                .ToList()
                .Select(x => new UsersViewItem
                {
                    UserId = x.Id,
                    UserName = x.UserName
                });

            return new UsersView
            {
                TotalCountByQuery = query.Invoke(this.UserRepository.Users).Count(),
                Users = filteredUsers.ToList()
            };
        }

        public InterviewersView GetInterviewers(int pageIndex, int pageSize, string orderBy, string searchBy, bool archived, bool? hasDevice, Guid? supervisorId)
        {
            Func<IQueryable<HqUser>, IQueryable<InterviewersItem>> query = allUsers =>
            {
                var interviewers = ApplyFilter(allUsers, searchBy, UserRoles.Interviewer, archived);

                if (hasDevice.HasValue)
                    interviewers = interviewers.Where(x => (x.Profile.DeviceId != null) == hasDevice.Value);

                if (supervisorId.HasValue)
                    interviewers = interviewers.Where(x => x.Profile.SupervisorId != null && x.Profile.SupervisorId == supervisorId);

                return interviewers.Select(x => new InterviewersItem
                {
                    UserId = x.Id,
                    CreationDate = x.CreationDate,
                    Email = x.Email,
                    IsLockedBySupervisor = x.IsLockedBySupervisor,
                    IsLockedByHQ = x.IsLockedByHeadquaters,
                    UserName = x.UserName,
                    SupervisorName = allUsers.FirstOrDefault(pr => pr.Id == x.Profile.SupervisorId).UserName,
                    DeviceId = x.Profile.DeviceId,
                    IsArchived = x.IsArchived
                });
            };

            orderBy = string.IsNullOrWhiteSpace(orderBy) ? nameof(HqUser.UserName) : orderBy;

            var filteredUsers = query
                .PagedAndOrderedQuery(orderBy, pageIndex, pageSize)
                .Invoke(this.UserRepository.Users)
                .ToList();

            return new InterviewersView
            {
                TotalCount = query.Invoke(this.UserRepository.Users).Count(),
                Items = filteredUsers.ToList()
            };
        }

        public UsersView GetAllSupervisors(int pageSize, string searchBy, bool showLocked = false)
        {
            Func<IQueryable<HqUser>, IQueryable<HqUser>> query = users =>
                ApplyFilter(users, searchBy, UserRoles.Supervisor, false)
                    .Where(user => showLocked || !user.IsLockedByHeadquaters);

            var filteredUsers = query
                .PagedAndOrderedQuery(nameof(HqUser.UserName), 1, pageSize)
                .Invoke(this.UserRepository.Users)
                .ToList()
                .Select(x => new UsersViewItem
                {
                    UserId = x.Id,
                    UserName = x.UserName
                });

            return new UsersView
            {
                TotalCountByQuery = query.Invoke(this.UserRepository.Users).Count(),
                Users = filteredUsers.ToList()
            };
        }

        public SupervisorsView GetSupervisors(int pageIndex, int pageSize, string orderBy, string searchBy, bool archived)
        {
            Func<IQueryable<HqUser>, IQueryable<SupervisorsQueryItem>> query =
                allUsers => ApplyFilter(allUsers, searchBy, UserRoles.Supervisor, archived)
                    .Select(supervisor => new SupervisorsQueryItem
                    {
                        IsLockedBySupervisor = supervisor.IsLockedBySupervisor,
                        IsLockedByHQ = supervisor.IsLockedByHeadquaters,
                        CreationDate = supervisor.CreationDate,
                        Email = supervisor.Email,
                        UserId = supervisor.Id,
                        UserName = supervisor.UserName,
                        InterviewersCount = allUsers.Count(pr => pr.Profile.SupervisorId == supervisor.Id && pr.IsArchived == false),
                        NotConnectedToDeviceInterviewersCount = allUsers.Count(pr => pr.Profile.SupervisorId == supervisor.Id && pr.Profile.DeviceId == null && pr.IsArchived == false)
                    });

            orderBy = string.IsNullOrWhiteSpace(orderBy) ? nameof(HqUser.UserName) : orderBy;

            List<SupervisorsQueryItem> usersPage = query.PagedAndOrderedQuery(orderBy, pageIndex, pageSize)
                .Invoke(this.UserRepository.Users)
                .ToList();

            var filteredUsers = usersPage.Select(x => new SupervisorsItem
            {
                IsLockedBySupervisor = x.IsLockedBySupervisor,
                IsLockedByHQ = x.IsLockedByHQ,
                CreationDate = x.CreationDate.FormatDateWithTime(),
                Email = x.Email,
                UserId = x.UserId,
                UserName = x.UserName,
                InterviewersCount = x.InterviewersCount,
                NotConnectedToDeviceInterviewersCount = x.NotConnectedToDeviceInterviewersCount
            }).ToList();

            return new SupervisorsView
            {
                TotalCount = query.Invoke(this.UserRepository.Users).Count(),
                Items = filteredUsers
            };
        }

        private static IQueryable<HqUser> ApplyFilter(IQueryable<HqUser> _, string searchBy, UserRoles role, bool archived)
        {
            var selectedRoleId = role.ToUserId();

            var allUsers = _.Where(x => x.IsArchived == archived && x.Roles.FirstOrDefault().RoleId == selectedRoleId);

            if (!string.IsNullOrWhiteSpace(searchBy))
            {
                var searchByToLower = searchBy.ToLower();
                allUsers = allUsers.Where(x => x.UserName.ToLower().Contains(searchByToLower) || x.Email.ToLower().Contains(searchByToLower));
            }
            return allUsers;
        }

        public class SupervisorsQueryItem
        {
            public int InterviewersCount { get; set; }

            public int NotConnectedToDeviceInterviewersCount { get; set; }
            public bool IsLockedBySupervisor { get; set; }
            public bool IsLockedByHQ { get; set; }
            public DateTime CreationDate { get; set; }
            public string UserName { get; set; }
            public Guid UserId { get; set; }
            public string Email { get; set; }
        }
    }
}