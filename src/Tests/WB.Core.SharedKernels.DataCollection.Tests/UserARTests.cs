﻿using System;
using System.Collections.Generic;
using System.Linq;
using Main.Core.Entities.SubEntities;
using Main.Core.Events.User;
using Microsoft.Practices.ServiceLocation;
using Moq;
using Ncqrs.Spec;
using NUnit.Framework;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Events.User;

namespace WB.Core.SharedKernels.DataCollection.Tests
{
    [TestFixture]
    public class UserARTests
    {
        private EventContext eventContext;

        [SetUp]
        public void Init()
        {
            ServiceLocator.SetLocatorProvider(() => new Mock<IServiceLocator> { DefaultValue = DefaultValue.Mock }.Object);
            this.eventContext = new EventContext();
        }

        [TearDown]
        public void Dispose()
        {
            this.eventContext.Dispose();
            this.eventContext = null;
        }

        [Test]
        public void Lock_When_called_Then_raised_UserLocked_event()
        {
            // arrange
            UserAR user = CreateUserAR();

            // act
            user.Lock();

            // assert
            Assert.That(this.GetRaisedEvents<UserLocked>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void Lock_When_called_Then_raised_UserLockedBySupervisor_event()
        {
            // arrange
            UserAR user = CreateUserAR();

            // act
            user.LockBySupervisor();

            // assert
            Assert.That(this.GetRaisedEvents<UserLockedBySupervisor>().Count(), Is.EqualTo(1));
        }


        [Test]
        public void Unlock_When_called_Then_raised_UserUnlocked_event()
        {
            // arrange
            UserAR user = CreateUserAR();

            // act
            user.Unlock();

            // assert
            Assert.That(this.GetRaisedEvents<UserUnlocked>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void Unlock_When_called_Then_raised_UserUnlockedBySupervisor_event()
        {
            // arrange
            UserAR user = CreateUserAR();

            // act
            user.UnlockBySupervisor();

            // assert
            Assert.That(this.GetRaisedEvents<UserUnlockedBySupervisor>().Count(), Is.EqualTo(1));
        }


        [Test]
        public void ChangeUser_When_is_locked_set_to_true_Then_raised_UserLockedBySupervisor_event()
        {
            // arrange
            UserAR user = CreateUserAR();
            bool isLockedBySupervisor = true;
            bool isLockedByHQ = false;

            // act
            user.ChangeUser("mail@domain.net", isLockedBySupervisor, isLockedByHQ, new UserRoles[] { }, string.Empty, Guid.Empty);

            // assert
            Assert.That(this.GetRaisedEvents<UserLockedBySupervisor>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void ChangeUser_When_is_locked_by_hq_set_to_true_Then_raised_UserLocked_event()
        {
            // arrange
            UserAR user = CreateUserAR();
            bool isLockedBySupervisor = false;
            bool isLockedByHQ = true;

            // act
            user.ChangeUser("mail@domain.net", isLockedBySupervisor, isLockedByHQ, new UserRoles[] { }, string.Empty, Guid.Empty);

            // assert
            Assert.That(this.GetRaisedEvents<UserLocked>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void ChangeUser_When_email_is_specified_Then_raised_UserChanged_event_with_specified_email()
        {
            // arrange
            UserAR user = CreateUserAR();
            string specifiedEmail = "user@example.com";

            // act
            user.ChangeUser(specifiedEmail, false, false, new UserRoles[] { }, string.Empty, Guid.Empty);

            // assert
            Assert.That(this.GetSingleRaisedEvent<UserChanged>().Email, Is.EqualTo(specifiedEmail));
        }

        [Test]
        public void ChangeUser_When_two_roles_are_specified_Then_raised_UserChanged_event_with_specified_roles()
        {
            // arrange
            UserAR user = CreateUserAR();
            IEnumerable<UserRoles> twoSpecifedRoles = new [] { UserRoles.Administrator, UserRoles.User };

            // act
            user.ChangeUser("mail@domain.net", false, false, twoSpecifedRoles.ToArray(), string.Empty, Guid.Empty);

            // assert
            Assert.That(this.GetSingleRaisedEvent<UserChanged>().Roles, Is.EquivalentTo(twoSpecifedRoles));
        }

        [Test]
        public void ctor_When_is_locked_set_to_true_Then_raised_NewUserCreated_event_with_is_locked_set_to_true()
        {
            // arrange
            bool isLockedBySupervisor = true;
            bool isLockedByHQ = false;

            // act
            new UserAR(Guid.NewGuid(), "name", "pwd", "my@email.com", new UserRoles[] { }, isLockedBySupervisor, isLockedByHQ, null);

            // assert
            Assert.That(this.GetSingleRaisedEvent<NewUserCreated>().IsLockedBySupervisor, Is.EqualTo(true));
        }

        [Test]
        public void ctor_When_name_is_specified_Then_raised_NewUserCreated_event_with_specified_name()
        {
            // arrange
            string specifiedName = "Green Lantern";

            // act
            new UserAR(Guid.NewGuid(), specifiedName, "pwd", "my@email.com", new UserRoles[] { }, false, false, null);

            // assert
            Assert.That(this.GetSingleRaisedEvent<NewUserCreated>().Name, Is.EqualTo(specifiedName));
        }

        [Test]
        public void ctor_When_password_is_specified_Then_raised_NewUserCreated_event_with_specified_password()
        {
            // arrange
            string specifiedPassword = "hhg<8923s:0";

            // act
            new UserAR(Guid.NewGuid(), "name", specifiedPassword, "my@email.com", new UserRoles[] { }, false, false, null);

            // assert
            Assert.That(this.GetSingleRaisedEvent<NewUserCreated>().Password, Is.EqualTo(specifiedPassword));
        }

        [Test]
        public void ctor_When_email_is_specified_Then_raised_NewUserCreated_event_with_specified_email()
        {
            // arrange
            string specifiedEmail = "gmail@chucknorris.com";

            // act
            new UserAR(Guid.NewGuid(), "name", "pwd", specifiedEmail, new UserRoles[] { }, false, false, null);

            // assert
            Assert.That(this.GetSingleRaisedEvent<NewUserCreated>().Email, Is.EqualTo(specifiedEmail));
        }

        [Test]
        public void ctor_When_public_key_is_specified_Then_raised_NewUserCreated_event_with_specified_public_key()
        {
            // arrange
            Guid specifiedPublicKey = Guid.NewGuid();

            // act
            new UserAR(specifiedPublicKey, "name", "pwd", "my@email.com", new UserRoles[] { }, false, false, null);

            // assert
            Assert.That(this.GetSingleRaisedEvent<NewUserCreated>().PublicKey, Is.EqualTo(specifiedPublicKey));
        }

        [Test]
        public void ctor_When_three_roles_are_specified_Then_raised_NewUserCreated_event_with_specified_roles()
        {
            // arrange
            IEnumerable<UserRoles> threeSpecifedRoles = new [] { UserRoles.Supervisor, UserRoles.Operator, UserRoles.User };

            // act
            new UserAR(Guid.NewGuid(), "name", "pwd", "my@email.com", threeSpecifedRoles.ToArray(), false, false, null);

            // assert
            Assert.That(this.GetSingleRaisedEvent<NewUserCreated>().Roles, Is.EquivalentTo(threeSpecifedRoles));
        }

        private static UserAR CreateUserAR()
        {
            Guid id = Guid.Parse("11111111111111111111111111111111");
            return new UserAR(id, "name", "pwd", "e@example.com", new UserRoles[] { }, false, false, null);
        }

        private T GetSingleRaisedEvent<T>()
        {
            return this.GetRaisedEvents<T>().Single();
        }

        private IEnumerable<T> GetRaisedEvents<T>()
        {
            return this.eventContext
                .Events
                .Where(e => e.Payload is T)
                .Select(e => e.Payload)
                .Cast<T>();
        }
    }
}