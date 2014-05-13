﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Machine.Specifications;
using Main.Core.Documents;
using Main.Core.Entities.Composite;
using Main.Core.Entities.SubEntities;
using Main.Core.Entities.SubEntities.Question;
using Microsoft.Practices.ServiceLocation;
using Moq;
using WB.Core.SharedKernels.DataCollection.DataTransferObjects.Synchronization;
using WB.Core.SharedKernels.DataCollection.ValueObjects;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;
using WB.Core.SharedKernels.DataCollection.Views.Questionnaire;
using WB.Core.SharedKernels.SurveyManagement.Views;
using WB.Core.SharedKernels.SurveyManagement.Views.Interview;
using WB.Core.SharedKernels.SurveyManagement.Views.Questionnaire;
using It = Machine.Specifications.It;

namespace WB.Core.SharedKernels.SurveyManagement.Tests.Merger
{
    internal class when_merging_questionnaire_and_interview_data_with_2nd_level_rosters_and_linked_question_on_it_at_parallel_roster_level : InterviewDataAndQuestionnaireMergerTestContext
    {
        Establish context = () =>
        {
            merger = CreateMerger();


            linkedQuestionId = Guid.Parse("10000000000000000000000000000000");
            sourceForLinkedQuestionId = Guid.Parse("11111111111111111111111111111111");
            firstLevelRosterId = Guid.Parse("10000000000000000000000000000000");
            secondLevelRosterId = Guid.Parse("44444444444444444444444444444444");

            parallelLevelRosterId = Guid.Parse("20000000000000000000000000000000");
            interviewId = Guid.Parse("43333333333333333333333333333333");

            questionnaireDocument = CreateQuestionnaireDocumentWithOneChapter(new Group()
            {
                PublicKey = firstLevelRosterId,
                IsRoster = true,
                RosterSizeSource = RosterSizeSourceType.FixedTitles,
                RosterFixedTitles = new[] { "roster1", "roster2" },
                Children = new List<IComposite>()
                {
                    new Group()
                    {
                        PublicKey = secondLevelRosterId,
                        IsRoster = true,
                        RosterSizeSource = RosterSizeSourceType.FixedTitles,
                        RosterFixedTitles = new[] { "t1", "t2" },
                        Children = new List<IComposite>()
                        {
                            new NumericQuestion()
                            {
                                PublicKey = sourceForLinkedQuestionId,
                                QuestionType = QuestionType.Numeric,
                                StataExportCaption = "sourceForLinkedQuestionId"
                            }
                        }
                    }
                }
            },
                new Group()
                {
                    PublicKey = parallelLevelRosterId,
                    IsRoster = true,
                    RosterSizeSource = RosterSizeSourceType.FixedTitles,
                    RosterFixedTitles = new[] { "parallel roster1", "parallel roster1" },
                    Children = new List<IComposite>()
                    {
                        new SingleQuestion()
                        {
                            PublicKey = linkedQuestionId,
                            LinkedToQuestionId = sourceForLinkedQuestionId,
                            StataExportCaption = "linkedQuestionId"
                        }
                    }
                });

            interview = CreateInterviewData(interviewId);

            AddInterviewLevel(interview, new ValueVector<Guid> { firstLevelRosterId }, new decimal[] { 0 }, new Dictionary<Guid, object>(),
                new Dictionary<Guid, string>() { { firstLevelRosterId, "roster1" } });
            AddInterviewLevel(interview, new ValueVector<Guid> { firstLevelRosterId }, new decimal[] { 1 }, new Dictionary<Guid, object>(),
                new Dictionary<Guid, string>() { { firstLevelRosterId, "roster2" } });

            AddInterviewLevel(interview, new ValueVector<Guid> { firstLevelRosterId, secondLevelRosterId }, new decimal[] { 0, 0 },
                new Dictionary<Guid, object> { { sourceForLinkedQuestionId, 11 } }, new Dictionary<Guid, string>() { { secondLevelRosterId, "roster11" } });
            AddInterviewLevel(interview, new ValueVector<Guid> { firstLevelRosterId, secondLevelRosterId }, new decimal[] { 0, 1 },
                new Dictionary<Guid, object> { { sourceForLinkedQuestionId, 12 } }, new Dictionary<Guid, string>() { { secondLevelRosterId, "roster12" } });
            AddInterviewLevel(interview, new ValueVector<Guid> { firstLevelRosterId, secondLevelRosterId }, new decimal[] { 1, 0 },
                new Dictionary<Guid, object> { { sourceForLinkedQuestionId, 21 } }, new Dictionary<Guid, string>() { { secondLevelRosterId, "roster21" } });


            AddInterviewLevel(interview, new ValueVector<Guid> { parallelLevelRosterId }, new decimal[] { 0 }, new Dictionary<Guid, object>(),
                new Dictionary<Guid, string>() { { parallelLevelRosterId, "paralel roster1" } });
            AddInterviewLevel(interview, new ValueVector<Guid> { parallelLevelRosterId }, new decimal[] { 1 }, new Dictionary<Guid, object>(),
               new Dictionary<Guid, string>() { { parallelLevelRosterId, "paralel roster2" } });

            questionnaire = CreateQuestionnaireWithVersion(questionnaireDocument);
            questionnaireReferenceInfo = CreateQuestionnaireReferenceInfo(questionnaireDocument);
            questionnaireRosters = CreateQuestionnaireRosterStructure(questionnaireDocument);
            user = Mock.Of<UserDocument>();
        };

        Because of = () =>
            mergeResult = merger.Merge(interview, questionnaire, questionnaireReferenceInfo, questionnaireRosters, user);

        It should_linked_in_first_row_has_3_options = () =>
            GetQuestion(mergeResult, linkedQuestionId, new decimal[] { 0 }).Options.Count().ShouldEqual(3);

        It should_linked_in_second_row_has_3_options = () =>
            GetQuestion(mergeResult, linkedQuestionId, new decimal[] { 1 }).Options.Count().ShouldEqual(3);

        It should_linked_question_in_first_row_has_first_option_equal_to_11 = () =>
            GetQuestion(mergeResult, linkedQuestionId, new decimal[] { 0 }).Options.First().Label.ShouldEqual("roster1: 11");

        It should_linked_question_in_first_row_has_second_option_equal_to_12 = () =>
            GetQuestion(mergeResult, linkedQuestionId, new decimal[] { 0 }).Options.ToArray()[1].Label.ShouldEqual("roster1: 12");

        It should_linked_question_in_first_row_has_third_option_equal_to_21 = () =>
            GetQuestion(mergeResult, linkedQuestionId, new decimal[] { 0 }).Options.Last().Label.ShouldEqual("roster2: 21");


        private static InterviewDataAndQuestionnaireMerger merger;
        private static InterviewDetailsView mergeResult;
        private static InterviewData interview;
        private static QuestionnaireDocumentVersioned questionnaire;
        private static ReferenceInfoForLinkedQuestions questionnaireReferenceInfo;
        private static QuestionnaireRosterStructure questionnaireRosters;
        private static UserDocument user;

        private static Guid firstLevelRosterId;
        private static Guid linkedQuestionId;
        private static Guid secondLevelRosterId;
        private static Guid sourceForLinkedQuestionId;
        private static Guid interviewId;
        private static Guid parallelLevelRosterId;
        private static QuestionnaireDocument questionnaireDocument;
    }
}
