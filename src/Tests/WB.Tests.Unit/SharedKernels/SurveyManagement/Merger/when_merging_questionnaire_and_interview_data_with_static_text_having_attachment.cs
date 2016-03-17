﻿using System;
using System.Collections.Generic;
using System.Linq;
using Machine.Specifications;
using Main.Core.Documents;
using Main.Core.Entities.Composite;
using Main.Core.Entities.SubEntities;
using Moq;
using WB.Core.SharedKernels.DataCollection.Views;
using WB.Core.SharedKernels.SurveyManagement.Views;
using WB.Core.SharedKernels.SurveyManagement.Views.Interview;
using WB.Core.SharedKernels.SurveyManagement.Views.Questionnaire;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.SharedKernels.SurveyManagement.Merger
{
    internal class when_merging_questionnaire_and_interview_data_with_static_text_having_attachment : InterviewDataAndQuestionnaireMergerTestContext
    {
        Establish context = () =>
        {
            questionnaire = CreateQuestionnaireDocumentWithOneChapter(
                new Group(nestedGroupTitle)
                {
                    PublicKey = nestedGroupId,
                    Children =
                        new List<IComposite>()
                        {
                            new StaticText(publicKey: staticTextId, text: staticText, attachmentName: attachmentName)
                        }
                });

            interview = CreateInterviewData(interviewId);
            
            user = Mock.Of<UserDocument>();

            merger = CreateMerger(questionnaire);
        };

        Because of = () =>
            mergeResult = merger.Merge(interview, questionnaire, user.GetUseLight(), null, new Dictionary<string, AttachmentInfoView>() { { attachmentName , new AttachmentInfoView(attachmentContentId, attachmentType) }});

        It should_static_text_exist= () =>
            GetStaticText().ShouldNotBeNull();

        It should_static_text_have_text = () =>
            GetStaticText().Text.ShouldEqual(staticText);

        It should_static_text_attachment_content_type = () =>
            GetStaticText().Attachment.ContentType.ShouldEqual(attachmentType);

        It should_static_text_attachment_content_id = () =>
            GetStaticText().Attachment.ContentId.ShouldEqual(attachmentContentId);

        private static InterviewGroupView GetNestedGroup()
        {
            return mergeResult.Groups.Find(g => g.Id == nestedGroupId);
        }

        private static InterviewStaticTextView GetStaticText()
        {
            return GetNestedGroup().Entities.OfType<InterviewStaticTextView>().FirstOrDefault(q => q.Id == staticTextId);
        }

        private static InterviewDataAndQuestionnaireMerger merger;
        private static InterviewDetailsView mergeResult;
        private static InterviewData interview;
        private static QuestionnaireDocument questionnaire;
        private static UserDocument user;
        private static Guid nestedGroupId = Guid.Parse("11111111111111111111111111111111");
        private static Guid staticTextId = Guid.Parse("55555555555555555555555555555555");
        private static Guid interviewId = Guid.Parse("33333333333333333333333333333333");
        private static string nestedGroupTitle = "nested Group";
        private static string staticText = "static text";
        private static string attachmentName = "test1";

        private static string attachmentType = "img";
        private static string attachmentContentId = "DTGHRHFJFJFJDD";
    }
}
