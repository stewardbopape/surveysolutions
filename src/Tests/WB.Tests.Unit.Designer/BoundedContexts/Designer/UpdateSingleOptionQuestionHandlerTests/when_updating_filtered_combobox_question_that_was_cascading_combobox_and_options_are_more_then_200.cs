using System;
using System.Linq;
using FluentAssertions;
using Main.Core.Entities.SubEntities;
using WB.Core.BoundedContexts.Designer.Aggregates;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire.Base;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire.Question;
using WB.Tests.Unit.Designer.BoundedContexts.QuestionnaireTests;

namespace WB.Tests.Unit.Designer.BoundedContexts.Designer.UpdateSingleOptionQuestionHandlerTests
{
    internal class when_updating_filtered_combobox_question_that_was_cascading_combobox_and_options_are_more_then_200 : QuestionnaireTestsContext
    {
        [NUnit.Framework.OneTimeSetUp] public void context () {
            int incrementer = 0;
            oldOptions = new Option[210].Select(
                answer =>
                    new Option
                    {
                        Value = incrementer.ToString(),
                        Title= (incrementer++).ToString(),
                        ParentValue = "1"
                    }).ToArray();

            questionnaire = CreateQuestionnaire(responsibleId: responsibleId);
            questionnaire.AddGroup(chapterId, responsibleId:responsibleId);
            questionnaire.AddSingleOptionQuestion
            (
                parentQuestionId,
                chapterId,
                options : new Option[] { new Option{ Title = "option1", Value = "1"},
                                         new Option(){Title= "option2", Value = "2"}},
                title : "Parent question",
                variableName : "cascade_parent",
                isPreFilled : false,
                responsibleId : responsibleId,
                linkedToQuestionId : null,
                isFilteredCombobox : false,
                cascadeFromQuestionId : null
            );

            questionnaire.AddSingleOptionQuestion
            (
                cascadeQuestionId,
                chapterId,
                options: oldOptions,
                title : "Cascade question",
                variableName: "cascade",
                isPreFilled: false,
                responsibleId : responsibleId,
                linkedToQuestionId : null,
                isFilteredCombobox : false,
                cascadeFromQuestionId : parentQuestionId
            );

            questionnaire.UpdateCascadingComboboxOptions(cascadeQuestionId, responsibleId, oldOptions.Select(x => Create.QuestionnaireCategoricalOption(int.Parse(x.Value), x.Title)).ToArray());
            BecauseOf();
        }

        private void BecauseOf() =>
            questionnaire.UpdateSingleOptionQuestion(
                new UpdateSingleOptionQuestion(
                    questionnaireId: questionnaire.Id,
                    questionId: cascadeQuestionId,
                    commonQuestionParameters: new CommonQuestionParameters()
                    {
                        Title = "title",
                        VariableName = "qr_barcode_question",
                        VariableLabel = null,
                        EnablementCondition = "some condition",
                        Instructions = "instructions",
                        HideIfDisabled = false
                    },

                    isPreFilled: false,
                    scope: QuestionScope.Interviewer,
                    responsibleId: responsibleId,
                    options: null,
                    linkedToEntityId: (Guid?)null,
                    isFilteredCombobox: true,
                    cascadeFromQuestionId: null,
                    validationConditions: new System.Collections.Generic.List<WB.Core.SharedKernels.QuestionnaireEntities.ValidationCondition>(),
                    linkedFilterExpression: null,
                    validationExpression: null,
                    validationMessage: null,
                    showAsList: false,
                    showAsListThreshold: null));


        [NUnit.Framework.Test] public void should_contains_question () =>
            questionnaire.QuestionnaireDocument.Find<IQuestion>(cascadeQuestionId);

        [NUnit.Framework.Test] public void should_contains_question_with_answer_option_that_was_presiously_saved () =>
            questionnaire.QuestionnaireDocument.Find<IQuestion>(cascadeQuestionId)
                .Answers.Count().Should().Be(oldOptions.Count());


        private static Questionnaire questionnaire;
        private static Guid cascadeQuestionId = Guid.Parse("11111111111111111111111111111111");
        private static Guid parentQuestionId = Guid.Parse("22222222222222222222222222222222");
        private static Guid chapterId = Guid.Parse("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC");
        private static Guid responsibleId = Guid.Parse("DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD");
        private static Option[] oldOptions;
    }
}
