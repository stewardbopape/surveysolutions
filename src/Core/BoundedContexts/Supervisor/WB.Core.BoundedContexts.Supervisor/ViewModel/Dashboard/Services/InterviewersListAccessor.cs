﻿using System;
using System.Collections.Generic;
using System.Linq;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure.Storage;
using WB.Core.SharedKernels.Enumerator.Views;

namespace WB.Core.BoundedContexts.Supervisor.ViewModel.Dashboard.Services
{
    public class InterviewersListAccessor : IInterviewersListAccessor
    {
        private readonly IPlainStorage<InterviewerDocument> interviewerViewRepository;

        public InterviewersListAccessor(IPlainStorage<InterviewerDocument> interviewerViewRepository)
        {
            this.interviewerViewRepository = interviewerViewRepository;
        }

        public List<InterviewerAssignInfo> GetInterviewers()
        {
            return interviewerViewRepository.LoadAll().Select(x =>
                new InterviewerAssignInfo
                {
                    Id = x.InterviewerId,
                    Login = x.UserName,
                    FullaName = x.FullaName
                }).ToList();
        }
    }

    public interface IInterviewersListAccessor
    {
        List<InterviewerAssignInfo> GetInterviewers();
    }

    public class InterviewerAssignInfo
    {
        public string Login { get; set; }
        public string FullaName { get; set; }
        public Guid Id { get; set; }
    }
}
