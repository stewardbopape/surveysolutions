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
    internal class when_verifying_questionnaire_that_has_question_with_option_filter_expression_about_10000_chars : QuestionnaireVerifierTestsContext
    {
        [NUnit.Framework.OneTimeSetUp] public void context () {
            questionnaire = CreateQuestionnaireDocument(new[]
            {
                Create.Group(groupId: groupId, children: new IComposite[]
                {
                    Create.SingleQuestion(questionId,
                        optionsFilter: "(@optioncode == 100) || (@optioncode == 200) || (@optioncode == 300) || (@optioncode == 400) || (@optioncode == 501) || (@optioncode == 502) || (@optioncode == 600) || (@optioncode == 701) || (@optioncode == 702) || (@optioncode == 801) || (@optioncode == 802) || (@optioncode == 901) || (@optioncode == 902) || (@optioncode == 903) || (@optioncode == 1001) || (@optioncode == 1002) || (@optioncode == 1101) || (@optioncode == 1102) || (@optioncode == 1201) || (@optioncode == 1202) || (@optioncode == 1301) || (@optioncode == 1302) || (@optioncode == 1401) || (@optioncode == 1402) || (@optioncode == 1403) || (@optioncode == 1404) || (@optioncode == 1405) || (@optioncode == 1406) || (@optioncode == 1407) || (@optioncode == 1408) || (@optioncode == 1409) || (@optioncode == 1410) || (@optioncode == 1411) || (@optioncode == 1412) || (@optioncode == 1413) || (@optioncode == 1500) || (@optioncode == 1601) || (@optioncode == 1602) || (@optioncode == 1701) || (@optioncode == 1702) || (@optioncode == 1703) || (@optioncode == 1800) || (@optioncode == 1901) || (@optioncode == 1902) || (@optioncode == 1903) || (@optioncode == 1904) || (@optioncode == 2000) || (@optioncode == 2101) || (@optioncode == 2102) || (@optioncode == 2103) || (@optioncode == 2104) || (@optioncode == 2105) || (@optioncode == 2106) || (@optioncode == 2107) || (@optioncode == 2201) || (@optioncode == 2202) || (@optioncode == 2203) || (@optioncode == 2301) || (@optioncode == 2302) || (@optioncode == 2303) || (@optioncode == 2304) || (@optioncode == 2305) || (@optioncode == 2401) || (@optioncode == 2402) || (@optioncode == 2403) || (@optioncode == 2404) || (@optioncode == 2501) || (@optioncode == 2502) || (@optioncode == 2503) || (@optioncode == 2504) || (@optioncode == 2505) || (@optioncode == 2601) || (@optioncode == 2602) || (@optioncode == 2603) || (@optioncode == 2604) || (@optioncode == 2605) || (@optioncode == 2606) || (@optioncode == 2607) || (@optioncode == 2608) || (@optioncode == 2701) || (@optioncode == 2702) || (@optioncode == 2703) || (@optioncode == 2704) || (@optioncode == 2705) || (@optioncode == 2706) || (@optioncode == 2801) || (@optioncode == 2802) || (@optioncode == 2803) || (@optioncode == 2804) || (@optioncode == 2805) || (@optioncode == 2806) || (@optioncode == 2807) || (@optioncode == 2808) || (@optioncode == 2809) || (@optioncode == 2810) || (@optioncode == 2901) || (@optioncode == 2902) || (@optioncode == 3001) || (@optioncode == 3002) || (@optioncode == 3101) || (@optioncode == 3102) || (@optioncode == 3103) || (@optioncode == 3104) || (@optioncode == 3105) || (@optioncode == 3201) || (@optioncode == 3202) || (@optioncode == 3203) || (@optioncode == 3301) || (@optioncode == 3302) || (@optioncode == 3401) || (@optioncode == 3402) || (@optioncode == 3403) || (@optioncode == 3404) || (@optioncode == 3405) || (@optioncode == 3406) || (@optioncode == 3407) || (@optioncode == 3408) || (@optioncode == 3409) || (@optioncode == 3410) || (@optioncode == 3501) || (@optioncode == 3502) || (@optioncode == 3503) || (@optioncode == 3504) || (@optioncode == 3505) || (@optioncode == 3506) || (@optioncode == 3507) || (@optioncode == 3508) || (@optioncode == 3509) || (@optioncode == 3601) || (@optioncode == 3602) || (@optioncode == 3701) || (@optioncode == 3702) || (@optioncode == 3703) || (@optioncode == 3704) || (@optioncode == 3705) || (@optioncode == 3706) || (@optioncode == 3801) || (@optioncode == 3802) || (@optioncode == 3803) || (@optioncode == 3804) || (@optioncode == 3805) || (@optioncode == 3901) || (@optioncode == 3902) || (@optioncode == 3903) || (@optioncode == 3904) || (@optioncode == 3905) || (@optioncode == 4001) || (@optioncode == 4002) || (@optioncode == 4003) || (@optioncode == 4004) || (@optioncode == 4005) || (@optioncode == 4006) || (@optioncode == 4007) || (@optioncode == 4100) || (@optioncode == 4201) || (@optioncode == 4202) || (@optioncode == 4203) || (@optioncode == 4204) || (@optioncode == 4301) || (@optioncode == 4302) || (@optioncode == 4304) || (@optioncode == 4401) || (@optioncode == 4402) || (@optioncode == 4403) || (@optioncode == 4404) || (@optioncode == 4501) || (@optioncode == 4502) || (@optioncode == 4503) || (@optioncode == 4504) || (@optioncode == 4600) || (@optioncode == 4701) || (@optioncode == 4702) || (@optioncode == 4801) || (@optioncode == 4802) || (@optioncode == 4803) || (@optioncode == 4804) || (@optioncode == 4805) || (@optioncode == 4806) || (@optioncode == 4807) || (@optioncode == 4808) || (@optioncode == 4809) || (@optioncode == 4811) || (@optioncode == 4901) || (@optioncode == 4902) || (@optioncode == 4903) || (@optioncode == 4904) || (@optioncode == 4905) || (@optioncode == 4906) || (@optioncode == 4907) || (@optioncode == 4908) || (@optioncode == 4909) || (@optioncode == 4910) || (@optioncode == 4911) || (@optioncode == 4912) || (@optioncode == 4913) || (@optioncode == 4914) || (@optioncode == 4915) || (@optioncode == 4916) || (@optioncode == 4917) || (@optioncode == 4918) || (@optioncode == 4919) || (@optioncode == 4920) || (@optioncode == 4921) || (@optioncode == 4922) || (@optioncode == 4923) || (@optioncode == 5001) || (@optioncode == 5002) || (@optioncode == 5003) || (@optioncode == 5004) || (@optioncode == 5005) || (@optioncode == 5006) || (@optioncode == 5100) || (@optioncode == 5200) || (@optioncode == 5301) || (@optioncode == 5302) || (@optioncode == 5400) || (@optioncode == 5500) || (@optioncode == 5600) || (@optioncode == 5700) || (@optioncode == 5801) || (@optioncode == 5802) || (@optioncode == 5901) || (@optioncode == 5902) || (@optioncode == 6001) || (@optioncode == 6002) || (@optioncode == 6101) || (@optioncode == 6102) || (@optioncode == 6201) || (@optioncode == 6202) || (@optioncode == 6203) || (@optioncode == 6204) || (@optioncode == 6205) || (@optioncode == 6301) || (@optioncode == 6302) || (@optioncode == 6401) || (@optioncode == 6402) || (@optioncode == 6501) || (@optioncode == 6502) || (@optioncode == 6503) || (@optioncode == 6504) || (@optioncode == 6601) || (@optioncode == 6602) || (@optioncode == 6701) || (@optioncode == 6702) || (@optioncode == 6703) || (@optioncode == 6704) || (@optioncode == 6801) || (@optioncode == 6802) || (@optioncode == 6901) || (@optioncode == 6902) || (@optioncode == 6903) || (@optioncode == 6904) || (@optioncode == 7001) || (@optioncode == 7002) || (@optioncode == 7101) || (@optioncode == 7102) || (@optioncode == 7103) || (@optioncode == 7200) || (@optioncode == 7301) || (@optioncode == 7302) || (@optioncode == 7400) || (@optioncode == 7501) || (@optioncode == 7502) || (@optioncode == 7503) || (@optioncode == 7600) || (@optioncode == 7700) || (@optioncode == 7800) || (@optioncode == 7900) || (@optioncode == 8001) || (@optioncode == 8002) || (@optioncode == 8101) || (@optioncode == 8102) || (@optioncode == 8103) || (@optioncode == 8200) || (@optioncode == 8300) || (@optioncode == 8400) || (@optioncode == 8501) || (@optioncode == 8502) || (@optioncode == 8601) || (@optioncode == 8602) || (@optioncode == 8700) || (@optioncode == 8801) || (@optioncode == 8802) || (@optioncode == 8901) || (@optioncode == 8902) || (@optioncode == 8903) || (@optioncode == 9001) || (@optioncode == 9002) || (@optioncode == 9003) || (@optioncode == 9004) || (@optioncode == 9005) || (@optioncode == 9101) || (@optioncode == 9102) || (@optioncode == 9200) || (@optioncode == 9300) || (@optioncode == 9401) || (@optioncode == 9402) || (@optioncode == 9403) || (@optioncode == 9500) || (@optioncode == 9601) || (@optioncode == 9602) || (@optioncode == 9701) || (@optioncode == 9702) || (@optioncode == 9801) || (@optioncode == 9802) || (@optioncode == 9900) || (@optioncode == 3602) || (@optioncode == 3701) || (@optioncode == 3702) || (@optioncode == 3703) || (@optioncode == 3704) || (@optioncode == 3705) || (@optioncode == 3706) || (@optioncode == 3801) || (@optioncode == 3802) || (@optioncode == 3803) || (@optioncode == 3804) || (@optioncode == 3805) || (@optioncode == 3901) || (@optioncode == 3902) || (@optioncode == 3903) || (@optioncode == 3904) || (@optioncode == 3905) || (@optioncode == 4001) || (@optioncode == 4002) || (@optioncode == 4003) || (@optioncode == 4004) || (@optioncode == 4005) || (@optioncode == 4006) || (@optioncode == 4007) || (@optioncode == 4100) || (@optioncode == 4201) || (@optioncode == 4202) || (@optioncode == 4203) || (@optioncode == 4204) || (@optioncode == 4301) || (@optioncode == 4302) || (@optioncode == 4304) || (@optioncode == 4401) || (@optioncode == 4402) || (@optioncode == 4403) || (@optioncode == 4404) || (@optioncode == 4501) || (@optioncode == 4502) || (@optioncode == 4503) || (@optioncode == 4504) || (@optioncode == 4600) || (@optioncode == 4701) || (@optioncode == 4702) || (@optioncode == 4801) || (@optioncode == 4802) || (@optioncode == 4803) || (@optioncode == 4804) || (@optioncode == 4805) || (@optioncode == 4806) || (@optioncode == 4807) || (@optioncode == 4808) || (@optioncode == 4809) || (@optioncode == 4811) || (@optioncode == 4901) || (@optioncode == 4902) || (@optioncode == 4903) || (@optioncode == 4904) || (@optioncode == 4905) || (@optioncode == 4906) || (@optioncode == 4907) || (@optioncode == 4908) || (@optioncode == 4909) || (@optioncode == 4910) || (@optioncode == 4911) || (@optioncode == 4912) || (@optioncode == 4913) || (@optioncode == 4914) || (@optioncode == 4915) || (@optioncode == 4916) || (@optioncode == 4917) || (@optioncode == 4918) || (@optioncode == 4919) || (@optioncode == 4920) || (@optioncode == 4921) || (@optioncode == 4922) || (@optioncode == 4923) || (@optioncode == 5001) || (@optioncode == 5002) || (@optioncode == 5003) || (@optioncode == 5004) || (@optioncode == 5005) || (@optioncode == 5006) || (@optioncode == 5100) || (@optioncode == 5200) || (@optioncode == 5301) || (@optioncode == 5302) || (@optioncode == 5400) || (@optioncode == 5500) || (@optioncode == 5600) || (@optioncode == 5700) || (@optioncode == 5801) || (@optioncode == 5802) || (@optioncode == 5901) || (@optioncode == 5902) || (@optioncode == 6001) || (@optioncode == 6002) || (@optioncode == 6101) || (@optioncode == 6102) || (@optioncode == 6201) || (@optioncode == 6202) || (@optioncode == 6203) || (@optioncode == 6204) || (@optioncode == 6205) || (@optioncode == 6301) || (@optioncode == 6302) || (@optioncode == 6401) || (@optioncode == 6402) || (@optioncode == 6501) || (@optioncode == 6502) || (@optioncode == 6503) || (@optioncode == 6504) || (@optioncode == 6601) || (@optioncode == 6602) || (@optioncode == 6701) || (@optioncode == 6702) || (@optioncode == 6703) || (@optioncode == 6704) || (@optioncode == 6801) || (@optioncode == 6802) || (@optioncode == 6901) || (@optioncode == 6902) || (@optioncode == 6903) || (@optioncode == 6904) || (@optioncode == 7001) || (@optioncode == 7002) || (@optioncode == 7101) || (@optioncode == 7102) || (@optioncode == 7103) || (@optioncode == 7200) || (@optioncode == 7301) || (@optioncode == 7302)")
                })
            });

            verifier = CreateQuestionnaireVerifier();
            BecauseOf();
        }

        private void BecauseOf() =>
            verificationMessages = verifier.CheckForErrors(Create.QuestionnaireView(questionnaire));

        [NUnit.Framework.Test] public void should_return_WB0028_message () =>
            verificationMessages.ShouldContainCritical("WB0028");

        [NUnit.Framework.Test] public void should_return_message_with_one_references () =>
            verificationMessages.GetCritical("WB0028").References.Count().ShouldEqual(1);

        [NUnit.Framework.Test] public void should_return_message_with_one_references_with_question_type () =>
            verificationMessages.GetCritical("WB0028").References.First().Type.ShouldEqual(QuestionnaireVerificationReferenceType.Question);

        [NUnit.Framework.Test] public void should_return_message_with_one_references_with_id_equals_questionId () =>
            verificationMessages.GetCritical("WB0028").References.First().Id.ShouldEqual(questionId);

        private static IEnumerable<QuestionnaireVerificationMessage> verificationMessages;
        private static QuestionnaireVerifier verifier;
        private static QuestionnaireDocument questionnaire;
        private static readonly Guid questionId = Guid.Parse("DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD");
        private static readonly Guid groupId = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
    }
}