using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Main.Core.Documents;
using Main.Core.Entities.Composite;
using WB.Core.BoundedContexts.Designer.ValueObjects;
using QuestionnaireVerifier = WB.Core.BoundedContexts.Designer.Verifier.QuestionnaireVerifier;

namespace WB.Tests.Unit.Designer.QuestionnaireVerificationTests
{
    internal class when_verifying_questionnaire_with_roster_group_that_contains_roster : QuestionnaireVerifierTestsContext
    {
        [NUnit.Framework.OneTimeSetUp] public void context () {
            rosterGroupId = Guid.Parse("10000000000000000000000000000000");
            var rosterSizeQiestionId = Guid.Parse("20000000000000000000000000000000");
            questionnaire = CreateQuestionnaireDocument(
                Create.NumericIntegerQuestion(rosterSizeQiestionId, variable: "var"), 
                Create.NumericRoster(rosterGroupId, variable: "a", rosterSizeQuestionId: rosterSizeQiestionId, children: new IComposite[]
                {
                    Create.NumericRoster(variable: "b", rosterSizeQuestionId: rosterSizeQiestionId)
                })
            );

            verifier = CreateQuestionnaireVerifier();
            BecauseOf();
        }

        private void BecauseOf() =>
            verificationMessages = verifier.CheckForErrors(Create.QuestionnaireView(questionnaire));

        [NUnit.Framework.Test] public void should_return_0_messages () =>
            verificationMessages.Count().Should().Be(0);

        private static IEnumerable<QuestionnaireVerificationMessage> verificationMessages;
        private static QuestionnaireVerifier verifier;
        private static QuestionnaireDocument questionnaire;
        private static Guid rosterGroupId;
    }
}
