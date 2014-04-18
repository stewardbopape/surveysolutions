﻿using Machine.Specifications;
using Moq;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.Edit.QuestionnaireInfo;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;
using It = Machine.Specifications.It;

namespace WB.Core.BoundedContexts.Designer.Tests.QuestionnaireInfoViewFactoryTests
{
    internal class when_loading_view : QuestionnaireInfoViewFactoryContext
    {
        Establish context = () =>
        {
            input = CreateQuestionnaireInfoViewInputModel(questionnaireId);

            var repositoryMock = new Mock<IQueryableReadSideRepositoryReader<QuestionnaireInfoView>>();

            repositoryMock
                .Setup(x => x.GetById(questionnaireId))
                .Returns(CreateQuestionnaireInfoView(questionnaireId, questionnaireTitle));

            factory = CreateQuestionnaireInfoViewFactory(repository: repositoryMock.Object);
        };

        Because of = () =>
            view = factory.Load(input);

        It should_find_questionnaire = () =>
            view.ShouldNotBeNull();

        It should_questionnaire_id_be_equal_questionnaireId = () =>
            view.QuestionnaireId.ShouldEqual(questionnaireId);

        It should_questionnaire_title_be_equal_questionnaireTitle = () =>
            view.Title.ShouldEqual(questionnaireTitle);

        private static QuestionnaireInfoView view;
        private static QuestionnaireInfoViewInputModel input;
        private static QuestionnaireInfoViewFactory factory;
        private static string questionnaireId = "11111111111111111111111111111111";
        private static string questionnaireTitle = "questionnaire title";
    }
}
