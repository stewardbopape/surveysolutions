using System;
using FluentAssertions;
using Main.Core.Documents;
using Moq;
using WB.Core.BoundedContexts.Designer.Services;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.Edit;
using WB.Core.Infrastructure.PlainStorage;


namespace WB.Tests.Unit.Designer.BoundedContexts.Designer.QuestionnaireInfoFactoryTests
{
    internal class when_getting_roster_edit_view_and_roster_is_absent : QuestionnaireInfoFactoryTestContext
    {
        [NUnit.Framework.OneTimeSetUp] public void context () {
            questionDetailsReaderMock = new Mock<IDesignerQuestionnaireStorage>();
            questionnaireView = CreateQuestionnaireDocument();
            questionDetailsReaderMock
                .Setup(x => x.Get(questionnaireId))
                .Returns(questionnaireView);

            factory = CreateQuestionnaireInfoFactory(questionDetailsReaderMock.Object);
            BecauseOf();
        }

        private void BecauseOf() =>
            result = factory.GetRosterEditView(questionnaireId, rosterId);

        [NUnit.Framework.Test] public void should_return_null () =>
            result.Should().BeNull();

        private static QuestionnaireInfoFactory factory;
        private static NewEditRosterView result;
        private static QuestionnaireDocument questionnaireView;
        private static Mock<IDesignerQuestionnaireStorage> questionDetailsReaderMock;
        private static Guid rosterId = Guid.Parse("BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB");
    }
}
