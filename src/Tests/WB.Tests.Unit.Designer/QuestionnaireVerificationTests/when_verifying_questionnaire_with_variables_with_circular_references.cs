using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Main.Core.Documents;
using Main.Core.Entities.Composite;
using WB.Core.BoundedContexts.Designer.ValueObjects;
using WB.Core.SharedKernels.QuestionnaireEntities;
using QuestionnaireVerifier = WB.Core.BoundedContexts.Designer.Verifier.QuestionnaireVerifier;


namespace WB.Tests.Unit.Designer.QuestionnaireVerificationTests
{
    internal class when_verifying_questionnaire_with_variables_with_circular_references : QuestionnaireVerifierTestsContext
    {
        [NUnit.Framework.OneTimeSetUp] public void context () {
            questionnaire = Create.QuestionnaireDocument(children: new IComposite[]
            {
                Create.Chapter(children: new IComposite[]
                {
                    Create.Variable(variable1Id, VariableType.LongInteger, "v1", expression: "v2 > 10"),
                    Create.Variable(variable2Id, VariableType.LongInteger, "v2", expression: "v1 == 8")
                })
            });

            verifier = CreateQuestionnaireVerifier();
            BecauseOf();
        }

        private void BecauseOf() =>
            verificationMessages = verifier.GetAllErrors(Create.QuestionnaireView(questionnaire));

        [NUnit.Framework.Test] public void should_return_contain_error_WB0056 () =>
            verificationMessages.ShouldContainError("WB0056");

        [NUnit.Framework.Test] public void should_return_message_with_level_general () =>
            verificationMessages.GetError("WB0056").MessageLevel.Should().Be(VerificationMessageLevel.General);

        [NUnit.Framework.Test] public void should_return_message_with_two_references () =>
            verificationMessages.GetError("WB0056").References.Count().Should().Be(2);

        [NUnit.Framework.Test] public void should_return_message_with_references_on_veriable1_and_2 () =>
            verificationMessages.GetError("WB0056").References.Select(x => x.Id).Should().Contain(new []{variable1Id, variable2Id});

        private static IEnumerable<QuestionnaireVerificationMessage> verificationMessages;
        private static QuestionnaireVerifier verifier;
        private static QuestionnaireDocument questionnaire;
        private static readonly Guid variable1Id = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        private static readonly Guid Question1Id = Guid.Parse("DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD");
        private static readonly Guid variable2Id = Guid.Parse("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF");
    }
}
