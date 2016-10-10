﻿using System;
using Machine.Specifications;
using Main.Core.Entities.SubEntities;
using WB.Core.BoundedContexts.Designer.Aggregates;
using WB.Core.BoundedContexts.Designer.Exceptions;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.Edit;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.QuestionnaireDto;

namespace WB.Tests.Unit.Designer.BoundedContexts.QuestionnaireTests
{
    internal class when_adding_roster_group_by_question_and_fixed_titles_is_specified : QuestionnaireTestsContext
    {
        Establish context = () =>
        {
            responsibleId = Guid.Parse("DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD");
            chapterId = Guid.Parse("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC");
            groupId = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
            rosterSizeQuestionId = Guid.Parse("11111111111111111111111111111111");
            rosterFixedTitles = new[] { new FixedRosterTitleItem("1", "fixed title") };

            questionnaire = CreateQuestionnaire(responsibleId: responsibleId);
            questionnaire.AddGroup(new NewGroupAdded { PublicKey = chapterId });
            questionnaire.AddMultiOptionQuestion(rosterSizeQuestionId,
                chapterId, responsibleId);
        };

        Because of = () =>
            exception =
                Catch.Exception(
                    () =>
                        questionnaire.AddGroupAndMoveIfNeeded(groupId: groupId, responsibleId: responsibleId, title: "title", variableName: null,
                            rosterSizeQuestionId: rosterSizeQuestionId, description: null, condition: null, hideIfDisabled: false,
                            parentGroupId: chapterId, isRoster: true, rosterSizeSource: RosterSizeSourceType.Question,
                            rosterFixedTitles: rosterFixedTitles, rosterTitleQuestionId: null));
        
        It should_throw_QuestionnaireException = () =>
            exception.ShouldBeOfExactType<QuestionnaireException>();

        It should_throw_exception_with_message = () =>
            new[] { "roster", "fixed", "items", "list", "should", "be", "empty" }.ShouldEachConformTo(keyword => exception.Message.ToLower().Contains(keyword));

        private static Exception exception;
        private static Questionnaire questionnaire;
        private static Guid responsibleId;
        private static Guid groupId;
        private static Guid chapterId;
        private static Guid rosterSizeQuestionId;
        private static FixedRosterTitleItem[] rosterFixedTitles;
    }
}