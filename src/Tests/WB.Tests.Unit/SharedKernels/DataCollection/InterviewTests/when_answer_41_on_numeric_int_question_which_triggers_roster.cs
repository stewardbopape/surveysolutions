﻿using System;
using Machine.Specifications;
using Main.Core.Entities.SubEntities;
using WB.Core.SharedKernels.DataCollection.Exceptions;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.SharedKernels.DataCollection.InterviewTests
{
    internal class when_answer_61_on_numeric_int_question_which_triggers_roster: InterviewTestsContext
    {
        Establish context = () =>
        {
            var questionnaireId = Guid.Parse("10000000000000000000000000000000");
            userId = Guid.Parse("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF");

            rosterSizeQuestionId = Guid.Parse("33333333333333333333333333333333");

            var questionnaire = CreateQuestionnaireDocumentWithOneChapter(
                Create.Entity.NumericQuestion(questionId: rosterSizeQuestionId, isInteger: true),
                Create.Entity.Roster(rosterId: Guid.Parse("11111111111111111111111111111111"),
                    rosterSizeSourceType: RosterSizeSourceType.Question, rosterSizeQuestionId: rosterSizeQuestionId));

            var questionnaireRepository = CreateQuestionnaireRepositoryStubWithOneQuestionnaire(questionnaireId,
                new PlainQuestionnaire(questionnaire, 1));

            interview = CreateInterview(questionnaireId: questionnaireId, questionnaireRepository: questionnaireRepository);
        };

        Because of = () =>
            exception = Catch.Only<InterviewException>(() => interview.AnswerNumericIntegerQuestion(userId, rosterSizeQuestionId, new decimal[0], DateTime.Now, 61));

        It should_throw_InterviewException = () =>
            exception.ShouldNotBeNull();

        It should_throw_InterviewException_with_explanation= () =>
           exception.Message.ToLower().ToSeparateWords().ShouldContain("answer", "'61'", "question", "roster", "greater", "60");

        private static Interview interview;
        private static Guid userId;
        private static Guid rosterSizeQuestionId;
        private static InterviewException exception;
    }
}