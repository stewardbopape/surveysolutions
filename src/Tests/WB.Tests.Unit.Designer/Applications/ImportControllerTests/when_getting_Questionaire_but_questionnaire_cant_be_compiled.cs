using System;
using System.Net;
using System.Web.Http;
using FluentAssertions;
using Main.Core.Documents;
using Moq;
using NUnit.Framework;
using WB.Core.BoundedContexts.Designer.Services;
using WB.Core.BoundedContexts.Designer.ValueObjects;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.Edit;
using WB.Core.SharedKernel.Structures.Synchronization.Designer;
using WB.UI.Designer.Api.Headquarters;


namespace WB.Tests.Unit.Designer.Applications.ImportControllerTests
{
    internal class when_getting_Questionaire_but_questionnaire_cant_be_compiled : ImportControllerTestContext
    {
        [NUnit.Framework.OneTimeSetUp] public void context () {
            request = Create.DownloadQuestionnaireRequest(questionnaireId);
            var questionnaireViewFactory = Mock.Of<IQuestionnaireViewFactory>(
                _ => _.Load(Moq.It.IsAny<QuestionnaireViewInputModel>()) == Create.QuestionnaireView(userId));

            var expressionsEngineVersionService = Mock.Of<IDesignerEngineVersionService>(
                _ => _.IsClientVersionSupported(Moq.It.IsAny<int>()) == true &&
                     _.GetListOfNewFeaturesForClient(Moq.It.IsAny<QuestionnaireDocument>(), Moq.It.IsAny<int>()) == new[] { "New questionnaire feature" });

            var questionnaireVerifier = Mock.Of<IQuestionnaireVerifier>(
                _ => _.CheckForErrors(Moq.It.IsAny<QuestionnaireView>()) == new QuestionnaireVerificationMessage[0]);

            string generatedAssembly;
            var expressionProcessorGenerator = Mock.Of<IExpressionProcessorGenerator>(
                _ => _.GenerateProcessorStateAssembly(Moq.It.IsAny<QuestionnaireDocument>(), Moq.It.IsAny<int>(),
                    out generatedAssembly) == Create.GenerationResult(false));

            importController = CreateImportController(
                questionnaireViewFactory: questionnaireViewFactory,
                engineVersionService: expressionsEngineVersionService,
                questionnaireVerifier: questionnaireVerifier,
                expressionProcessorGenerator: expressionProcessorGenerator);
            importController.SetupLoggedInUser(userId);
            BecauseOf();
        }

        private void BecauseOf() =>
            exception = Assert.Throws<HttpResponseException>(() =>
                importController.Questionnaire(request));

        [NUnit.Framework.Test] public void should_throw_HttpResponseException () =>
            exception.Should().NotBeNull();

        [NUnit.Framework.Test] public void should_throw_HttpResponseException_with_StatusCode_UpgradeRequired () =>
            exception.Response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        [NUnit.Framework.Test] public void should_throw_HttpResponseException_with_explanation_in_ReasonPhrase () =>
            exception.Response.ReasonPhrase.ToLower().ToSeparateWords().Should().Contain("questionnaire", "contains", "functionality", "not", "supported", "update");

        private static ImportV2Controller importController;
        private static HttpResponseException exception;
        private static DownloadQuestionnaireRequest request;
        private static Guid questionnaireId = Guid.Parse("22222222222222222222222222222222");
        private static Guid userId = Guid.Parse("33333333333333333333333333333333");
    }
}
