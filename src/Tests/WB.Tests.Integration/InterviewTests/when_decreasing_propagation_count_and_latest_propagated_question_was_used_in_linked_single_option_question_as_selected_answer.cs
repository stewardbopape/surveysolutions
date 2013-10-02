﻿using System;
using System.Collections.Generic;
using Machine.Specifications;
using Main.Core.Documents;
using Main.Core.Entities.Composite;
using Main.Core.Entities.SubEntities;
using Main.Core.Entities.SubEntities.Question;
using Microsoft.Practices.ServiceLocation;
using Moq;
using Ncqrs.Spec;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates;
using WB.Core.SharedKernels.DataCollection.Implementation.Repositories;
using It = Machine.Specifications.It;
using System.Linq;

namespace WB.Tests.Integration.InterviewTests
{
    internal class when_decreasing_propagation_count_and_latest_propagated_question_was_used_in_linked_single_option_question_as_selected_answer : InterviewTestsContext
    {
        Establish context = () =>
        {
            userId = Guid.Parse("AAAA0000AAAA00000000AAAA0000AAAA");
            answerTime = new DateTime(2013, 10, 02);

            numericQuestionId = Guid.Parse("11111111111111111111111111111111");
            var propagatableGroupId = Guid.Parse("22222222222222222222222222222222");
            var referencedQuestionId = Guid.Parse("22220000000000000000000000000000");
            linkedQuestionId = Guid.Parse("33333333333333333333333333333333");

            var questionnaireDocument = new QuestionnaireDocument
            {
                Children = new List<IComposite>
                {
                    new Group("Chapter")
                    {
                        Children = new List<IComposite>
                        {
                            new AutoPropagateQuestion
                            {
                                PublicKey = numericQuestionId,
                                QuestionType = QuestionType.Numeric,
                                MaxValue = 10,
                                Triggers = new List<Guid> {propagatableGroupId},
                            },
                            new Group
                            {
                                PublicKey = propagatableGroupId,
                                Propagated = Propagate.AutoPropagated,
                                Children = new List<IComposite>
                                {
                                    new TextQuestion
                                    {
                                        PublicKey = referencedQuestionId,
                                        QuestionType = QuestionType.Text,
                                    }
                                }
                            },
                            new SingleQuestion
                            {
                                PublicKey = linkedQuestionId,
                                QuestionType = QuestionType.SingleOption,
                                LinkedToQuestionId = referencedQuestionId,
                            },
                        }
                    }
                }
            };

            interview = CreateInterviewFromQuestionnaireDocumentRegisteringAllNeededDependencies(questionnaireDocument);
            interview.AnswerNumericQuestion(userId, numericQuestionId, new int[] {}, answerTime, 3);
            interview.AnswerTextQuestion(userId, referencedQuestionId, new int[] { 0 }, answerTime, "A");
            interview.AnswerTextQuestion(userId, referencedQuestionId, new int[] { 2 }, answerTime, "C");
            interview.AnswerSingleOptionLinkedQuestion(userId, linkedQuestionId, new int[] {}, answerTime, new int[] { 2 });

            eventContext = new EventContext();
        };

        Because of = () =>
            interview.AnswerNumericQuestion(userId, numericQuestionId, new int[] {}, answerTime, 2);

        It should_raise_AnswerRemoved_event_with_linked_single_option_id_and_propagation_vector = () =>
            eventContext.ShouldContainEvent<AnswerRemoved>(@event
                => @event.QuestionId == linkedQuestionId
                && @event.PropagationVector.SequenceEqual(new int[] {}));

        Cleanup stuff = () =>
        {
            eventContext.Dispose();
            eventContext = null;
        };

        private static EventContext eventContext;
        private static Interview interview;
        private static Guid userId;
        private static Guid numericQuestionId;
        private static DateTime answerTime;
        private static Guid linkedQuestionId;
    }
}