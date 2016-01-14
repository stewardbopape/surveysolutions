﻿using System;
using Machine.Specifications;
using Moq;
using WB.Core.Infrastructure.ReadSide;
using WB.Core.SharedKernels.DataCollection.Views.Questionnaire;
using WB.Core.SharedKernels.SurveyManagement.Factories;
using WB.Core.SharedKernels.SurveyManagement.Web.Api;
using WB.Core.SharedKernels.SurveyManagement.Web.Models.Api;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.SharedKernels.SurveyManagement.Web.ApiTests
{
    internal class when_questionnaires_controller_questionnaries_by_id : ApiTestContext
    {
        private Establish context = () =>
        {
            questionnaireBrowseViewFactoryMock = new Mock<IQuestionnaireBrowseViewFactory>();

            controller = CreateQuestionnairesController(questionnaireBrowseViewViewFactory: questionnaireBrowseViewFactoryMock.Object);
        };
        
        Because of = () =>
        {
            actionResult = controller.Questionnaires(questionnaireId, 10, 1);
        };

        It should_return_QuestionnaireApiView = () =>
            actionResult.ShouldBeOfExactType<QuestionnaireApiView>();

        It should_call_factory_load_once = () =>
            questionnaireBrowseViewFactoryMock.Verify(x => x.Load(Moq.It.IsAny<QuestionnaireBrowseInputModel>()), Times.Once());

        private static Guid questionnaireId = Guid.Parse("11111111111111111111111111111111");
        private static QuestionnaireApiView actionResult;
        private static QuestionnairesController controller;
        private static Mock<IQuestionnaireBrowseViewFactory> questionnaireBrowseViewFactoryMock;
    }
}
