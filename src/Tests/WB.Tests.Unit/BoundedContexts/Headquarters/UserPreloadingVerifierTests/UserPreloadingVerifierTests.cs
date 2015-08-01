﻿using System;
using Main.Core.Entities.SubEntities;
using Main.DenormalizerStorage;
using Moq;
using NUnit.Framework;
using WB.Core.BoundedContexts.Headquarters.UserPreloading;
using WB.Core.BoundedContexts.Headquarters.UserPreloading.Dto;
using WB.Core.BoundedContexts.Headquarters.UserPreloading.Services;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;
using WB.Core.Infrastructure.Transactions;
using WB.Core.SharedKernels.DataCollection.Views;

namespace WB.Tests.Unit.BoundedContexts.Headquarters.UserPreloadingVerifierTests
{
    [TestFixture]
    public class UserPreloadingVerifierTests
    {
        [Test]
        public void
            VerifyProcessFromReadyToBeVerifiedQueue_When_login_is_taken_by_existing_user_Then_record_verification_error_with_code_PLU0001()
        {
            var userName = "nastya";
            var userStorage = new InMemoryReadSideRepositoryAccessor<UserDocument>();
            userStorage.Store(Create.UserDocument(userName: userName), "id");
            var userPreloadingProcess = Create.UserPreloadingProcess(Create.UserPreloadingDataRecord(userName));
            var userPreloadingServiceMock = CreateUserPreloadingServiceMock(userPreloadingProcess);

            var userPreloadingVerifier =
                CreateUserPreloadingVerifier(userPreloadingService: userPreloadingServiceMock.Object, userStorage: userStorage);

            userPreloadingVerifier.VerifyProcessFromReadyToBeVerifiedQueue();

            userPreloadingServiceMock.Verify(x => x.PushVerificationError(userPreloadingProcess.UserPreloadingProcessId, "PLU0001", 1, "Login", userName));
            userPreloadingServiceMock.Verify(x => x.UpdateVerificationProgressInPercents(userPreloadingProcess.UserPreloadingProcessId, 8));
        }

        [Test]
        public void
            VerifyProcessFromReadyToBeVerifiedQueue_When_2_users_with_the_same_login_are_present_in_the_dataset_Then_record_verification_error_with_code_PLU0002()
        {
            var userName = "nastya";
            var userPreloadingProcess = Create.UserPreloadingProcess(Create.UserPreloadingDataRecord(userName), Create.UserPreloadingDataRecord(userName));
            var userPreloadingServiceMock = CreateUserPreloadingServiceMock(userPreloadingProcess);

            var userPreloadingVerifier =
                CreateUserPreloadingVerifier(userPreloadingService: userPreloadingServiceMock.Object);

            userPreloadingVerifier.VerifyProcessFromReadyToBeVerifiedQueue();

            userPreloadingServiceMock.Verify(x => x.PushVerificationError(userPreloadingProcess.UserPreloadingProcessId, "PLU0002", 1, "Login", userName));
            userPreloadingServiceMock.Verify(x => x.PushVerificationError(userPreloadingProcess.UserPreloadingProcessId, "PLU0002", 2, "Login", userName));
        }

        [Test]
        public void
            VerifyProcessFromReadyToBeVerifiedQueue_When_login_is_taken_by_archived_interviewer_in_other_team_Then_record_verification_error_with_code_PLU0003()
        {
            var userName = "nastya";
            var supervisorName = "super";
            var userStorage = new InMemoryReadSideRepositoryAccessor<UserDocument>();
            userStorage.Store(Create.UserDocument(userName: userName, supervisorId: Guid.NewGuid(), isArchived:true), "id1");
            userStorage.Store(Create.UserDocument(userName: supervisorName), "id2");
            var userPreloadingProcess = Create.UserPreloadingProcess(Create.UserPreloadingDataRecord(login: userName, supervisor: supervisorName));
            var userPreloadingServiceMock = CreateUserPreloadingServiceMock(userPreloadingProcess);

            var userPreloadingVerifier =
                CreateUserPreloadingVerifier(userPreloadingService: userPreloadingServiceMock.Object, userStorage: userStorage);

            userPreloadingVerifier.VerifyProcessFromReadyToBeVerifiedQueue();

            userPreloadingServiceMock.Verify(x => x.PushVerificationError(userPreloadingProcess.UserPreloadingProcessId, "PLU0003", 1, "Login", userName));
        }

        [Test]
        public void
            VerifyProcessFromReadyToBeVerifiedQueue_When_login_is_taken_by_user_in_other_role_Then_record_verification_error_with_code_PLU0004()
        {
            var userName = "nastya";
            var userStorage = new InMemoryReadSideRepositoryAccessor<UserDocument>();
            userStorage.Store(Create.UserDocument(userName: userName, isArchived:true), "id");
            var userPreloadingProcess = Create.UserPreloadingProcess(Create.UserPreloadingDataRecord(userName));
            var userPreloadingServiceMock = CreateUserPreloadingServiceMock(userPreloadingProcess);

            var userPreloadingVerifier =
                CreateUserPreloadingVerifier(userPreloadingService: userPreloadingServiceMock.Object, userStorage: userStorage);

            userPreloadingVerifier.VerifyProcessFromReadyToBeVerifiedQueue();

            userPreloadingServiceMock.Verify(x => x.PushVerificationError(userPreloadingProcess.UserPreloadingProcessId, "PLU0004", 1, "Login", userName));
        }

        [Test]
        public void
            VerifyProcessFromReadyToBeVerifiedQueue_When_users_login_contains_invalid_characted_Then_record_verification_error_with_code_PLU0005()
        {
            var userName = "na$tya";
            var userPreloadingProcess = Create.UserPreloadingProcess(Create.UserPreloadingDataRecord(userName));
            var userPreloadingServiceMock = CreateUserPreloadingServiceMock(userPreloadingProcess);

            var userPreloadingVerifier =
                CreateUserPreloadingVerifier(userPreloadingService: userPreloadingServiceMock.Object);

            userPreloadingVerifier.VerifyProcessFromReadyToBeVerifiedQueue();

            userPreloadingServiceMock.Verify(x => x.PushVerificationError(userPreloadingProcess.UserPreloadingProcessId, "PLU0005", 1, "Login", userName));
        }

        [Test]
        public void
            VerifyProcessFromReadyToBeVerifiedQueue_When_users_password_is_empty_Then_record_verification_error_with_code_PLU0006()
        {
            var emptyPassword = "";
            var userPreloadingProcess = Create.UserPreloadingProcess(Create.UserPreloadingDataRecord(password: emptyPassword));
            var userPreloadingServiceMock = CreateUserPreloadingServiceMock(userPreloadingProcess);

            var userPreloadingVerifier =
                CreateUserPreloadingVerifier(userPreloadingService: userPreloadingServiceMock.Object);

            userPreloadingVerifier.VerifyProcessFromReadyToBeVerifiedQueue();

            userPreloadingServiceMock.Verify(x => x.PushVerificationError(userPreloadingProcess.UserPreloadingProcessId, "PLU0006", 1, "Password", emptyPassword));
        }

        [Test]
        public void
            VerifyProcessFromReadyToBeVerifiedQueue_When_users_email_contains_invalid_characted_Then_record_verification_error_with_code_PLU0007()
        {
            var email = "na$tya";
            var userPreloadingProcess = Create.UserPreloadingProcess(Create.UserPreloadingDataRecord(email: email));
            var userPreloadingServiceMock = CreateUserPreloadingServiceMock(userPreloadingProcess);

            var userPreloadingVerifier =
                CreateUserPreloadingVerifier(userPreloadingService: userPreloadingServiceMock.Object);

            userPreloadingVerifier.VerifyProcessFromReadyToBeVerifiedQueue();

            userPreloadingServiceMock.Verify(x => x.PushVerificationError(userPreloadingProcess.UserPreloadingProcessId, "PLU0007", 1, "Email", email));
        }

        [Test]
        public void
            VerifyProcessFromReadyToBeVerifiedQueue_When_users_phone_number_contains_invalid_characted_Then_record_verification_error_with_code_PLU0008()
        {
            var phoneNumber = "na$tya";
            var userPreloadingProcess = Create.UserPreloadingProcess(Create.UserPreloadingDataRecord(phoneNumber: phoneNumber));
            var userPreloadingServiceMock = CreateUserPreloadingServiceMock(userPreloadingProcess);

            var userPreloadingVerifier =
                CreateUserPreloadingVerifier(userPreloadingService: userPreloadingServiceMock.Object);

            userPreloadingVerifier.VerifyProcessFromReadyToBeVerifiedQueue();

            userPreloadingServiceMock.Verify(x => x.PushVerificationError(userPreloadingProcess.UserPreloadingProcessId, "PLU0008", 1, "PhoneNumber", phoneNumber));
        }

        [Test]
        public void
            VerifyProcessFromReadyToBeVerifiedQueue_When_users_role_is_undefined_Then_record_verification_error_with_code_PLU0009()
        {
            var userPreloadingProcess = Create.UserPreloadingProcess(Create.UserPreloadingDataRecord());
            var userPreloadingServiceMock = CreateUserPreloadingServiceMock(userPreloadingProcess, role: UserRoles.Undefined);

            var userPreloadingVerifier =
                CreateUserPreloadingVerifier(userPreloadingServiceMock.Object);

            userPreloadingVerifier.VerifyProcessFromReadyToBeVerifiedQueue();

            userPreloadingServiceMock.Verify(x => x.PushVerificationError(userPreloadingProcess.UserPreloadingProcessId, "PLU0009", 1, "Role", "supervisor"));
        }

        [Test]
        public void
            VerifyProcessFromReadyToBeVerifiedQueue_When_user_in_role_interviewer_has_supervisor_in_role_supervisor_Then_record_verification_error_with_code_PLU0010()
        {
            var interviewerName = "int";
            var supervisorName = "super";
            var userPreloadingProcess = Create.UserPreloadingProcess(
                Create.UserPreloadingDataRecord(login: interviewerName, supervisor: supervisorName),
                Create.UserPreloadingDataRecord(login: supervisorName, supervisor: interviewerName));

            var userPreloadingServiceMock = CreateUserPreloadingServiceMock(userPreloadingProcess);

            var userPreloadingVerifier =
                CreateUserPreloadingVerifier(userPreloadingServiceMock.Object);

            userPreloadingVerifier.VerifyProcessFromReadyToBeVerifiedQueue();

            userPreloadingServiceMock.Verify(x => x.PushVerificationError(userPreloadingProcess.UserPreloadingProcessId, "PLU0010", 1, "Supervisor", supervisorName));
        }

        [Test]
        public void
            VerifyProcessFromReadyToBeVerifiedQueue_When_user_in_role_supervisor_has_not_empty_supervisor_column_Then_record_verification_error_with_code_PLU0011()
        {
            var supervisorName = "super";
            var supervisorCellValue = "super_test";
            var userPreloadingProcess = Create.UserPreloadingProcess(
                Create.UserPreloadingDataRecord(login: supervisorName, supervisor: supervisorCellValue));

            var userPreloadingServiceMock = CreateUserPreloadingServiceMock(userPreloadingProcess, UserRoles.Supervisor);

            var userPreloadingVerifier =
                CreateUserPreloadingVerifier(userPreloadingServiceMock.Object);

            userPreloadingVerifier.VerifyProcessFromReadyToBeVerifiedQueue();

            userPreloadingServiceMock.Verify(x => x.PushVerificationError(userPreloadingProcess.UserPreloadingProcessId, "PLU0011", 1, "Supervisor", supervisorCellValue));
        }

        private UserPreloadingVerifier CreateUserPreloadingVerifier(
            IUserPreloadingService userPreloadingService = null, 
            IQueryableReadSideRepositoryReader<UserDocument> userStorage = null)
        {
            return
                new UserPreloadingVerifier(
                    Mock.Of<ITransactionManagerProvider>(
                        _ => _.GetTransactionManager() == Mock.Of<ITransactionManager>()),
                    userPreloadingService ?? Mock.Of<IUserPreloadingService>(),
                    userStorage ?? new InMemoryReadSideRepositoryAccessor<UserDocument>(),
                    Mock.Of<IPlainTransactionManager>(), new UserPreloadingSettings(5, 5, 12, 1, 10000, 100));
        }

        private Mock<IUserPreloadingService> CreateUserPreloadingServiceMock(UserPreloadingProcess userPreloadingProcess, UserRoles role = UserRoles.Operator)
        {
            var userPreloadingServiceMock = new Mock<IUserPreloadingService>();
            userPreloadingServiceMock.Setup(x => x.DeQueuePreloadingProcessIdReadyToBeValidated()).Returns(userPreloadingProcess.UserPreloadingProcessId);
            userPreloadingServiceMock.Setup(x => x.GetPreloadingProcesseDetails(userPreloadingProcess.UserPreloadingProcessId))
                .Returns(userPreloadingProcess);

            userPreloadingServiceMock.Setup(x => x.GetUserRoleFromDataRecord(Moq.It.IsAny<UserPreloadingDataRecord>()))
                .Returns(role);

            return userPreloadingServiceMock;
        }
    }

}