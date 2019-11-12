using System;
using Main.Core.Documents;
using Main.Core.Entities.Composite;
using Ncqrs.Spec;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates;
using WB.Core.SharedKernels.QuestionnaireEntities;
using WB.Tests.Abc;

namespace WB.Tests.Integration.InterviewTests.Variables
{
    internal class when_Answering_question_which_changes_value_of_a_variable : InterviewTestsContext
    {
        [NUnit.Framework.OneTimeSetUp] public void context () {
            appDomainContext = AppDomainContext.Create();

            var questionnaireId = Guid.Parse("10000000000000000000000000000000");
            userId = Guid.Parse("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF");
            textQuetionId = Guid.Parse("21111111111111111111111111111111");
            variableId = Guid.Parse("22222222222222222222222222222222");

            QuestionnaireDocument questionnaire = Create.Entity.QuestionnaireDocumentWithOneChapter(id: questionnaireId,
                children: new IComposite[]
                {
                    Create.Entity.TextQuestion(textQuetionId, variable: "txt"),
                    Create.Entity.Variable(variableId, VariableType.LongInteger, "v1", "txt.Length")
                });

            interview = SetupInterview(appDomainContext.AssemblyLoadContext, questionnaire);
            eventContext = new EventContext();

            BecauseOf();
        }

        [NUnit.Framework.OneTimeTearDown] public void CleanUp()
        {
            eventContext.Dispose();
            eventContext = null;
            appDomainContext.Dispose();
        }

        public void BecauseOf() =>
            interview.AnswerTextQuestion(userId, textQuetionId, new decimal[0], DateTime.Now, "Nastya");

        [NUnit.Framework.Test] public void should_raise_VariablesValuesChanged_event_for_the_variable () =>
            eventContext.ShouldContainEvent<VariablesChanged>(@event
                => (long?) @event.ChangedVariables[0].NewValue == 6 && @event.ChangedVariables[0].Identity.Id== variableId);

        private static AppDomainContext appDomainContext;
        private static EventContext eventContext;
        private static Interview interview;
        private static Guid userId;
        private static Guid variableId;
        private static Guid textQuetionId;
    }
}
