﻿using Machine.Specifications;
using Main.Core.Entities.SubEntities;
using Moq;
using WB.Core.BoundedContexts.Designer.Services;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.Edit.ChapterInfo;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.Designer.BoundedContexts.Designer.ChapterInfoViewDenormalizerTests
{
    internal class when_TextListQuestionCloned_event_received : ChaptersInfoViewDenormalizerTestContext
    {
        Establish context = () =>
        {
            var expressionProcessor = new Mock<IExpressionProcessor>();
            expressionProcessor.Setup(x => x.GetIdentifiersUsedInExpression(questionConditionExpression))
                .Returns(new[] { variableUsedInQuestionConditionExpression });

            viewState = CreateGroupInfoViewWith1ChapterAnd1QuestionInsideChapter(chapterId, sourceQuestionId);
            denormalizer = CreateDenormalizer(view: viewState, expressionProcessor: expressionProcessor.Object);
        };

        Because of = () =>
            viewState =
                denormalizer.Update(viewState,
                    Create.Event.TextListQuestionClonedEvent(questionId: questionId, parentGroupId: chapterId,
                        questionVariable: questionVariable, questionTitle: questionTitle,
                        questionConditionExpression: questionConditionExpression, sourceQuestionId: sourceQuestionId));

        It should_groupInfoView_first_chapter_items_not_be_null = () =>
            ((GroupInfoView)viewState.Items[0]).Items.ShouldNotBeNull();

        It should_groupInfoView_first_chapter_items_count_be_equal_to_2 = () =>
            ((GroupInfoView)viewState.Items[0]).Items.Count.ShouldEqual(2);

        It should_groupInfoView_first_chapter_first_item_type_be_equal_to_QuestionInfoView = () =>
            ((GroupInfoView)viewState.Items[0]).Items[0].ShouldBeOfExactType<QuestionInfoView>();

        It should_groupInfoView_first_chapter_first_item_id_be_equal_to_questionId = () =>
            ((GroupInfoView)viewState.Items[0]).Items[0].ItemId.ShouldEqual(questionId);

        It should_groupInfoView_first_chapter_first_question_title_be_equal_to_questionTitle = () =>
            ((QuestionInfoView)((GroupInfoView)viewState.Items[0]).Items[0]).Title.ShouldEqual(questionTitle);

        It should_groupInfoView_first_chapter_first_question_variable_be_equal_to_questionVariable = () =>
            ((QuestionInfoView)((GroupInfoView)viewState.Items[0]).Items[0]).Variable.ShouldEqual(questionVariable);

        It should_groupInfoView_first_chapter_first_question_type_be_equal_to_questionType = () =>
            ((QuestionInfoView)((GroupInfoView)viewState.Items[0]).Items[0]).Type.ShouldEqual(QuestionType.TextList);

        private static string chapterId = "33333333333333333333333333333333";
        private static string questionId = "22222222222222222222222222222222";
        private static string sourceQuestionId = "11111111111111111111111111111111";
        private static string questionTitle = "question title";
        private static string questionVariable = "var";
        private static string variableUsedInQuestionConditionExpression = "var1";
        private static string questionConditionExpression = string.Format("[{0}] > 0", variableUsedInQuestionConditionExpression);
        
        private static ChaptersInfoViewDenormalizer denormalizer;
        private static GroupInfoView viewState;
    }
}
