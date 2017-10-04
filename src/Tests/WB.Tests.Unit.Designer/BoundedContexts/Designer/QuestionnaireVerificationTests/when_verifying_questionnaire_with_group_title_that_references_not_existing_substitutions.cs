using System;
using System.Collections.Generic;
using System.Linq;
using Machine.Specifications;
using Main.Core.Documents;
using Main.Core.Entities.Composite;
using WB.Core.BoundedContexts.Designer.Implementation.Services;
using WB.Core.BoundedContexts.Designer.ValueObjects;
using QuestionnaireVerifier = WB.Core.BoundedContexts.Designer.Verifier.QuestionnaireVerifier;

namespace WB.Tests.Unit.Designer.BoundedContexts.Designer.QuestionnaireVerificationTests
{
    internal class when_verifying_questionnaire_with_group_title_that_references_not_existing_substitutions : QuestionnaireVerifierTestsContext
    {
        [NUnit.Framework.OneTimeSetUp] public void context () {
            questionnaire = Create.QuestionnaireDocument(children: new IComposite[]
            {
                Create.Group(groupId: groupId, title: "hello %unknown%"),
            });

            verifier = CreateQuestionnaireVerifier();
            BecauseOf();
        }

        private void BecauseOf() =>
            verificationMessages = verifier.CheckForErrors(Create.QuestionnaireView(questionnaire));

        [NUnit.Framework.Test] public void should_return_error_with_code_WB0017 () =>
            verificationMessages.GetError("WB0017").ShouldNotBeNull();

        [NUnit.Framework.Test] public void should_return_error_WB0017_with_1_reference () =>
            verificationMessages.GetError("WB0017").References.Count.ShouldEqual(1);

        [NUnit.Framework.Test] public void should_return_error_WB0017_with_reference_with_type_Group () =>
            verificationMessages.GetError("WB0017").References.Single().Type.ShouldEqual(QuestionnaireVerificationReferenceType.Group);

        [NUnit.Framework.Test] public void should_return_error_WB0017_with_reference_with_group_id () =>
            verificationMessages.GetError("WB0017").References.Single().Id.ShouldEqual(groupId);

        private static IEnumerable<QuestionnaireVerificationMessage> verificationMessages;
        private static QuestionnaireVerifier verifier;
        private static QuestionnaireDocument questionnaire;

        private static Guid groupId = Guid.Parse("10000000000000000000000000000000");
    }
}