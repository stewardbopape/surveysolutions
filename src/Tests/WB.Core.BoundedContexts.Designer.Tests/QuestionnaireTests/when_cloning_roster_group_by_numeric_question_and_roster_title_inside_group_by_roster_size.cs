﻿using System;
using Machine.Specifications;
using Main.Core.Entities.SubEntities;
using Main.Core.Events.Questionnaire;
using Ncqrs.Spec;
using WB.Core.BoundedContexts.Designer.Aggregates;
using WB.Core.BoundedContexts.Designer.Events.Questionnaire;

namespace WB.Core.BoundedContexts.Designer.Tests.QuestionnaireTests
{
    internal class when_cloning_roster_group_by_numeric_question_and_roster_title_inside_group_by_roster_size : QuestionnaireTestsContext
    {
        Establish context = () =>
        {
            responsibleId = Guid.Parse("DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD");
            var chapterId = Guid.Parse("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC");
            parentGroupId = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
            groupId = Guid.Parse("BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB");
            rosterTitleQuestionId = Guid.Parse("1BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB");
            rosterSizeQuestionId = Guid.Parse("2BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB");
            rosterGroupWithRosterTitleQuestionId = Guid.Parse("3BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB");
            newGroupId = Guid.Parse("4BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB");
            rosterSizeSourceType = RosterSizeSourceType.Question;

            questionnaire = CreateQuestionnaire(responsibleId: responsibleId);
            questionnaire.Apply(new NewGroupAdded { PublicKey = chapterId });
            questionnaire.Apply(new NewGroupAdded { PublicKey = groupId, ParentGroupPublicKey = chapterId });
            questionnaire.Apply(new NewGroupAdded { PublicKey = rosterGroupWithRosterTitleQuestionId, ParentGroupPublicKey = chapterId });
            questionnaire.Apply(new GroupBecameARoster(responsibleId: responsibleId, groupId: rosterGroupWithRosterTitleQuestionId));
            questionnaire.Apply(new RosterChanged(responsibleId: responsibleId, groupId: rosterGroupWithRosterTitleQuestionId,
                rosterFixedTitles: null, rosterSizeSource: RosterSizeSourceType.Question, rosterSizeQuestionId: rosterSizeQuestionId,
                rosterTitleQuestionId: null));
            questionnaire.Apply(new NewQuestionAdded()
            {
                PublicKey = rosterTitleQuestionId,
                GroupPublicKey = rosterGroupWithRosterTitleQuestionId,
                QuestionType = QuestionType.Text
            });
            questionnaire.Apply(new NewQuestionAdded()
            {
                PublicKey = rosterSizeQuestionId,
                GroupPublicKey = chapterId,
                QuestionType = QuestionType.Numeric,
                IsInteger = true
            });
            questionnaire.Apply(new NewGroupAdded { PublicKey = parentGroupId });

            eventContext = new EventContext();
        };

        Because of = () =>
            questionnaire.CloneGroupWithoutChildren(groupId: newGroupId, responsibleId: responsibleId, title: "title",
                description: null, condition: null, rosterSizeQuestionId: rosterSizeQuestionId,
                isRoster: true, rosterSizeSource: rosterSizeSourceType, rosterFixedTitles: null,
                rosterTitleQuestionId: rosterTitleQuestionId, parentGroupId: groupId, targetIndex: 0, sourceGroupId: groupId);

        Cleanup stuff = () =>
        {
            eventContext.Dispose();
            eventContext = null;
        };


        It should_raise_GroupBecameARoster_event_with_GroupId_specified = () =>
            eventContext.GetSingleEvent<GroupBecameARoster>()
                .GroupId.ShouldEqual(newGroupId);

        It should_raise_RosterChanged_event_with_GroupId_specified = () =>
            eventContext.GetSingleEvent<RosterChanged>()
                .GroupId.ShouldEqual(newGroupId);

        It should_raise_RosterChanged_event_with_RosterSizeQuestionId_equal_to_specified_question_id = () =>
            eventContext.GetSingleEvent<RosterChanged>()
                .RosterSizeQuestionId.ShouldEqual(rosterSizeQuestionId);

        It should_raise_RosterChanged_event_with_RosterTitleQuestionId_equal_to_specified_question_id = () =>
            eventContext.GetSingleEvent<RosterChanged>()
                .RosterTitleQuestionId.ShouldEqual(rosterTitleQuestionId);

        private static EventContext eventContext;
        private static Questionnaire questionnaire;
        private static Guid responsibleId;
        private static Guid groupId;
        private static Guid newGroupId;
        private static Guid rosterTitleQuestionId;
        private static Guid rosterSizeQuestionId;
        private static Guid parentGroupId;
        private static Guid rosterGroupWithRosterTitleQuestionId;
        private static RosterSizeSourceType rosterSizeSourceType;
    }
}