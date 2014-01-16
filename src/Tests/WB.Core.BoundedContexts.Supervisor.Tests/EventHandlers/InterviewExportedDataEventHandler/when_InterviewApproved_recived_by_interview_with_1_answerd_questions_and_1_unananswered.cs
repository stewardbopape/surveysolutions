﻿using System;
using System.Collections.Generic;
using System.Linq;
using Machine.Specifications;
using Main.Core.Documents;
using WB.Core.BoundedContexts.Supervisor.Views.DataExport;

namespace WB.Core.BoundedContexts.Supervisor.Tests.EventHandlers.InterviewExportedDataEventHandler
{
    internal class when_InterviewApproved_recived_by_interview_with_1_answerd_questions_and_1_unananswered :
        InterviewExportedDataEventHandlerTestContext
    {
        Establish context = () =>
        {
            answeredQuestionId = Guid.Parse("10000000000000000000000000000000");
            unansweredQuestionId = Guid.Parse("11111111111111111111111111111111");

            variableNameAndQuestionId = new Dictionary<string, Guid>
            {
                { "q1", answeredQuestionId },
                { "q2", unansweredQuestionId }
            };
            questionnaireDocument = CreateQuestionnaireDocument(variableNameAndQuestionId);
            interviewExportedDataEventHandler = CreateInterviewExportedDataEventHandlerForQuestionnarieCreatedByMethod(
                () => questionnaireDocument,
                () => CreateInterviewWithAnswers(variableNameAndQuestionId.Values.Take(1)));
        };

        Because of = () =>
            result = interviewExportedDataEventHandler.Create(CreatePublishableEvent());

        It should_records_count_equals_1 = () =>
            result.Levels[0].Records.Length.ShouldEqual(1);

        It should__first_record_have_1_answers = () =>
            result.Levels[0].Records[0].Questions.Length.ShouldEqual(2);

        It should_first_record_id_equals_0 = () =>
            result.Levels[0].Records[0].RecordId.ShouldEqual(0);

        It should_answered_question_be_not_empty = () =>
           result.Levels[0].Records[0].Questions.ShouldQuestionHasOneNotEmptyAnswer(answeredQuestionId);

        It should_unanswered_question_be_empty = () =>
          result.Levels[0].Records[0].Questions.ShouldQuestionHasNoAnswers(unansweredQuestionId);

        private static EventHandler.InterviewExportedDataEventHandler interviewExportedDataEventHandler;
        private static InterviewDataExportView result;
        private static Dictionary<string, Guid> variableNameAndQuestionId;
        private static Guid answeredQuestionId;
        private static Guid unansweredQuestionId;
        private static QuestionnaireDocument questionnaireDocument;
    }
}
