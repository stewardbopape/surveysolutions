﻿using System;
using Machine.Specifications;
using Main.Core.Documents;
using Moq;
using WB.Core.BoundedContexts.Interviewer.ChangeLog;
using WB.Core.BoundedContexts.Interviewer.Implementation.Services;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.SharedKernel.Structures.Synchronization;
using WB.Core.SharedKernel.Structures.Synchronization.SurveyManagement;
using WB.Core.SharedKernels.DataCollection.Commands.Questionnaire;
using WB.Core.SharedKernels.DataCollection.Repositories;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.BoundedContexts.Interviewer.CapiDataSynchronizationServiceTests
{
    internal class when_sync_package_contains_information_about_questionnaire_with_broken_metadata : CapiDataSynchronizationServiceTestContext
    {
        Establish context = () =>
        {
            questionnaireDocument = new QuestionnaireDocument()
            {
                PublicKey = Guid.NewGuid()
            };

            syncItem = new QuestionnaireSyncPackageDto
                       {
                           Content = "some content", MetaInfo = "some metadata"
                       };

            var jsonUtilsMock = new Mock<IJsonUtils>();
            jsonUtilsMock.Setup(x => x.Deserialize<QuestionnaireDocument>(syncItem.Content)).Returns(questionnaireDocument);
            jsonUtilsMock.Setup(x => x.Deserialize<QuestionnaireMetadata>(syncItem.MetaInfo)).Throws<NullReferenceException>();

            commandService = new Mock<ICommandService>();

            plainQuestionnaireRepositoryMock = new Mock<IPlainQuestionnaireRepository>();
            changeLogManipulator = new Mock<IChangeLogManipulator>();
            capiDataSynchronizationService = CreateCapiDataSynchronizationService(changeLogManipulator.Object, commandService.Object, jsonUtilsMock.Object, null,
                plainQuestionnaireRepositoryMock.Object);
        };

        Because of = () => exception = Catch.Exception(() => capiDataSynchronizationService.ProcessDownloadedPackage(syncItem, SyncItemType.Questionnaire));

        It should_not_call_RegisterPlainQuestionnaire =
            () =>
                commandService.Verify(
                    x =>
                        x.Execute(
                            Moq.It.Is<RegisterPlainQuestionnaire>(
                                param =>
                                    param.QuestionnaireId == questionnaireDocument.PublicKey && param.Version == Moq.It.IsAny<long>()), null),
                    Times.Never);

        It should_not_store_questionnaire_in_pline_storage =
            () =>
                plainQuestionnaireRepositoryMock.Verify(
                    x => x.StoreQuestionnaire(questionnaireDocument.PublicKey, Moq.It.IsAny<long>(), questionnaireDocument),
                    Times.Never);

        It should_throw_ArgumentException = () =>
            exception.ShouldBeOfExactType<ArgumentException>();

        private static CapiDataSynchronizationService capiDataSynchronizationService;
        private static QuestionnaireSyncPackageDto syncItem;
        private static QuestionnaireDocument questionnaireDocument;
        private static Mock<ICommandService> commandService;
        private static Mock<IPlainQuestionnaireRepository> plainQuestionnaireRepositoryMock;
        private static Exception exception;
        private static Mock<IChangeLogManipulator> changeLogManipulator;
    }
}
