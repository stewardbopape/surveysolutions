﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Machine.Specifications;
using Moq;
using WB.Core.BoundedContexts.Designer.Views.Questionnaire.ChangeHistory;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;
using WB.Tests.Unit.SharedKernels.SurveyManagement;
using It = Machine.Specifications.It;

namespace WB.Tests.Unit.BoundedContexts.Designer.QuestionnaireChangeHistoryDenormalizerTests
{
    internal class when_questionnaire_item_is_moving : QuestionnaireChangeHistoryDenormalizerTestContext
    {
        Establish context = () =>
        {
            var questionnaireStateTacker = Create.QuestionnaireStateTacker();
            questionnaireStateTacker.StaticTextState.Add(Guid.Parse(staticTextId),"");
            questionnaireStateTacker.GroupsState.Add(Guid.Parse(questionnaireId), "");
            questionnaireStateTacker.GroupsState.Add(Guid.Parse(groupId), "");
            questionnaireStateTacker.QuestionsState.Add(Guid.Parse(questionId), "");
            questionnaireStateTackerStorage =
                Mock.Of<IReadSideKeyValueStorage<QuestionnaireStateTacker>>(
                    _ => _.GetById(Moq.It.IsAny<string>()) == questionnaireStateTacker);
            questionnaireChangeRecordStorage = new TestInMemoryWriter<QuestionnaireChangeRecord>();

            questionnaireChangeHistoryDenormalizer =
                CreateQuestionnaireChangeHistoryDenormalizer(questionnaireStateTacker: questionnaireStateTackerStorage,
                    questionnaireChangeRecord: questionnaireChangeRecordStorage);
        };

        Because of = () =>
        {
            questionnaireChangeHistoryDenormalizer.Handle(Create.QuestionnaireItemMovedEvent(staticTextId, questionnaireId: questionnaireId));
            questionnaireChangeHistoryDenormalizer.Handle(Create.QuestionnaireItemMovedEvent(groupId, questionnaireId: questionnaireId));
            questionnaireChangeHistoryDenormalizer.Handle(Create.QuestionnaireItemMovedEvent(questionId, questionnaireId: questionnaireId));
        };

        It should_store_3_changes = () =>
           GetAllRecords(questionnaireChangeRecordStorage).Length.ShouldEqual(3);

        It should_store_first_change_for_static_text = () =>
            GetAllRecords(questionnaireChangeRecordStorage)[0].TargetItemType.ShouldEqual(QuestionnaireItemType.StaticText);

        It should_store_second_change_for_group = () =>
            GetAllRecords(questionnaireChangeRecordStorage)[1].TargetItemType.ShouldEqual(QuestionnaireItemType.Group);

        It should_store_third_change_for_question = () =>
           GetAllRecords(questionnaireChangeRecordStorage)[2].TargetItemType.ShouldEqual(QuestionnaireItemType.Question);

        private static QuestionnaireChangeHistoryDenormalizer questionnaireChangeHistoryDenormalizer;
        private static string staticTextId = "11111111111111111111111111111111";
        private static string groupId = "22222222222222222222222222222222";
        private static string questionId = "33333333333333333333333333333333";
        private static string questionnaireId = "44444444444444444444444444444444";

        private static IReadSideKeyValueStorage<QuestionnaireStateTacker> questionnaireStateTackerStorage;
        private static TestInMemoryWriter<QuestionnaireChangeRecord> questionnaireChangeRecordStorage;
    }
}
