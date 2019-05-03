using System;
using Main.Core.Documents;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using WB.Core.BoundedContexts.Designer.Services;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.Edit;
using WB.UI.Designer.Controllers.Api.Headquarters;

namespace WB.Tests.Unit.Designer.Api.Headquarters.QuestionnairesControllerTests
{
    internal class when_getting_Questionnaire_but_client_version_lower_than_server_one : QuestionnairesControllerTestContext
    {
        [Test]
        public void should_throw_HttpResponseException_with_explanation_in_ReasonPhrase()
        {

            var questionnaireViewFactory = Mock.Of<IQuestionnaireViewFactory>(
                _ => _.Load(It.IsAny<QuestionnaireViewInputModel>()) == Create.QuestionnaireView(userId));

            newQuestionnaireFeatureDescription = "variables";
            var expressionsEngineVersionService = Mock.Of<IDesignerEngineVersionService>(
                _ => _.IsClientVersionSupported(Moq.It.IsAny<int>()) == true &&
                     _.GetListOfNewFeaturesForClient(Moq.It.IsAny<QuestionnaireDocument>(), It.IsAny<int>()) == new[] { newQuestionnaireFeatureDescription });

            questionnairesController = CreateQuestionnairesController(
                questionnaireViewFactory: questionnaireViewFactory,
                engineVersionService: expressionsEngineVersionService);

            questionnairesController.SetupLoggedInUser(userId);
            var result = questionnairesController.Get(questionnaireId, 12, null) as ObjectResult;

            Assert.That(result.StatusCode, Is.EqualTo(StatusCodes.Status417ExpectationFailed));
            Assert.That(result.Value, Is.EqualTo("Your questionnaire \"\" contains new functionality: \"variables\". New feature(s) is not supported by your installation. Please update."));
        }

        private static HQQuestionnairesController questionnairesController;
        private static readonly Guid questionnaireId = Guid.Parse("22222222222222222222222222222222");
        private static readonly Guid userId = Guid.Parse("33333333333333333333333333333333");
        private static string newQuestionnaireFeatureDescription;
    }
}
