﻿using System;
using System.Linq;
using Machine.Specifications;
using Main.Core.Entities.SubEntities;
using Moq;
using Ncqrs.Spec;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Events.Interview;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates;
using WB.Core.SharedKernels.DataCollection.Implementation.Repositories;
using WB.Core.SharedKernels.DataCollection.Repositories;
using It = Machine.Specifications.It;

namespace WB.Core.SharedKernels.DataCollection.Tests.InterviewTests
{
    internal class when_answering_datetime_question_which_is_roster_title_for_2_rosters_and_roster_level_is_1_and_answer_is__08_Mar_2011__ : InterviewTestsContext
    {
        Establish context = () =>
        {
            userId = Guid.Parse("FFFFFFFFFFFFFFFFFFFFFF1111111111");
            var questionnaireId = Guid.Parse("DDDDDDDDDDDDDDDDDDDDDD0000000000");

            emptyRosterVector = new decimal[] { };
            var rosterInstanceId = (decimal)22.5;
            rosterVector = emptyRosterVector.Concat(new[] { rosterInstanceId }).ToArray();

            questionId = Guid.Parse("11111111111111111111111111111111");
            rosterAId = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
            rosterBId = Guid.Parse("BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB");


            var questionnaire = Mock.Of<IQuestionnaire>
            (_
                => _.HasQuestion(questionId) == true
                && _.GetQuestionType(questionId) == QuestionType.DateTime
                && _.GetRostersFromTopToSpecifiedQuestion(questionId) == new[] { rosterAId }
                && _.DoesQuestionSpecifyRosterTitle(questionId) == true
                && _.GetRostersAffectedByRosterTitleQuestion(questionId) == new[] { rosterAId, rosterBId }
            );


            SetupInstanceToMockedServiceLocator<IQuestionnaireRepository>(
                CreateQuestionnaireRepositoryStubWithOneQuestionnaire(questionnaireId, questionnaire));

            interview = CreateInterview(questionnaireId: questionnaireId);
            interview.Apply(new RosterRowAdded(rosterAId, emptyRosterVector, rosterInstanceId, sortIndex: null));
            interview.Apply(new RosterRowAdded(rosterBId, emptyRosterVector, rosterInstanceId, sortIndex: null));

            eventContext = new EventContext();
        };

        Because of = () =>
            interview.AnswerDateTimeQuestion(userId, questionId, rosterVector, DateTime.Now, new DateTime(2011, 03, 08));

        Cleanup stuff = () =>
        {
            eventContext.Dispose();
            eventContext = null;
        };

        It should_raise_DateTimeQuestionAnswered_event = () =>
            eventContext.ShouldContainEvent<DateTimeQuestionAnswered>();

        It should_raise_1_RosterRowsTitleChanged_events = () =>
            eventContext.ShouldContainEvents<RosterInstancesTitleChanged>(count: 1);

        It should_set_2_affected_roster_ids_in_RosterRowsTitleChanged_events = () =>
            eventContext.GetEvents<RosterInstancesTitleChanged>().SelectMany(@event => @event.ChangedInstances.Select(r=>r.RosterInstance.GroupId)).ToArray()
                .ShouldContainOnly(rosterAId, rosterBId);

        It should_set_empty_outer_roster_vector_to_all_RosterRowTitleChanged_events = () =>
            eventContext.GetEvents<RosterRowTitleChanged>()
                .ShouldEachConformTo(@event => Enumerable.SequenceEqual(@event.OuterRosterVector, emptyRosterVector));

        It should_set_last_element_of_roster_vector_to_roster_instance_id_in_all_RosterRowTitleChanged_events = () =>
            eventContext.GetEvents<RosterRowTitleChanged>()
                .ShouldEachConformTo(@event => @event.RosterInstanceId == rosterVector.Last());

        It should_set_title_to__3_slash_8_slash_2011__in_all_RosterRowTitleChanged_events = () =>
            eventContext.GetEvents<RosterRowTitleChanged>().Select(@event => @event.Title)
                .ShouldEachConformTo(title => title == "3/8/2011");

        private static EventContext eventContext;
        private static Interview interview;
        private static Guid userId;
        private static Guid questionId;
        private static decimal[] rosterVector;
        private static decimal[] emptyRosterVector;
        private static Guid rosterAId;
        private static Guid rosterBId;
    }
}