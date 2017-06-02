﻿using System;
using System.Collections.Generic;
using System.Linq;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.ExpressionStorage;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates.InterviewEntities.Answers;

namespace WB.Core.SharedKernels.DataCollection.Implementation.Aggregates.InterviewEntities
{
    public interface IInterviewNodesUpdater
    {
        void UpdateEnablement(IInterviewTreeNode entity);
        void UpdateEnablement(InterviewTreeGroup entity);

        void UpdateSingleOptionQuestion(InterviewTreeQuestion question);
        void UpdateMultiOptionQuestion(InterviewTreeQuestion question);
        void UpdateYesNoQuestion(InterviewTreeQuestion question);
        void UpdateCascadingQuestion(InterviewTreeQuestion question);
        void UpdateLinkedQuestion(InterviewTreeQuestion question);
        void UpdateLinkedToListQuestion(InterviewTreeQuestion question);

        void UpdateRoster(InterviewTreeRoster roster);
        void UpdateVariable(InterviewTreeVariable variable);
        void UpdateValidations(InterviewTreeStaticText staticText);
        void UpdateValidations(InterviewTreeQuestion question);
    }

    public class InterviewNodesUpdater : IInterviewNodesUpdater
    {
        private readonly IInterviewExpressionStorage expressionStorage;
        private readonly IQuestionnaire questionnaire;
        private readonly Identity questionnaireIdentity;
        private readonly bool removeLinkedAnswers;

        readonly HashSet<Identity> disabledNodes = new HashSet<Identity>();

        public InterviewNodesUpdater(IInterviewExpressionStorage expressionStorage, IQuestionnaire questionnaire,
            bool removeLinkedAnswers)
        {
            this.expressionStorage = expressionStorage;
            this.questionnaire = questionnaire;
            this.questionnaireIdentity = new Identity(questionnaire.QuestionnaireId, RosterVector.Empty);
            this.removeLinkedAnswers = removeLinkedAnswers;
        }

        public void UpdateEnablement(IInterviewTreeNode entity)
        {
            if (disabledNodes.Contains(entity.Identity))
                return;

            var level = GetLevel(entity);
            var result = RunConditionExpression(level.GetConditionExpression(entity.Identity));
            if (result)
                entity.Enable();
            else
                entity.Disable();
        }

        public void UpdateEnablement(InterviewTreeGroup group)
        {
            if (disabledNodes.Contains(group.Identity))
                return;

            var level = GetLevel(group);
            var result = RunConditionExpression(level.GetConditionExpression(group.Identity));
            if (result)
                group.Enable();
            else
            {
                group.Disable();
                List<Identity> disabledChildNodes = group.DisableChildNodes();
                disabledChildNodes.ForEach(x => disabledNodes.Add(x));
            }
        }

        public void UpdateSingleOptionQuestion(InterviewTreeQuestion question)
        {
            if (disabledNodes.Contains(question.Identity))
                return;

            if (!(question.IsAnswered() && questionnaire.IsSupportFilteringForOptions(question.Identity.Id)))
                return;

                var level = GetLevel(question);
            var filter = level.GetCategoricalFilter(question.Identity);
            var filterResult = RunOptionFilter(filter,
                question.AsSingleFixedOption.GetAnswer().SelectedValue);
            if (!filterResult)
                question.RemoveAnswer();
        }

        public void UpdateMultiOptionQuestion(InterviewTreeQuestion question)
        {
            if (disabledNodes.Contains(question.Identity))
                return;

            if (!(question.IsAnswered() && questionnaire.IsSupportFilteringForOptions(question.Identity.Id)))
                return;

            var level = GetLevel(question);
            var filter = level.GetCategoricalFilter(question.Identity);
            var selectedOptions =
                question.AsMultiFixedOption.GetAnswer().CheckedValues.ToArray();
            var newSelectedOptions =
                selectedOptions.Where(x => RunOptionFilter(filter, x)).ToArray();
            if (newSelectedOptions.Length != selectedOptions.Length)
            {
                question.AsMultiFixedOption.SetAnswer(
                    CategoricalFixedMultiOptionAnswer.FromInts(newSelectedOptions));
                // remove rosters, implement cheaper solutions
                question.Tree.ActualizeTree();
            }
        }

        public void UpdateYesNoQuestion(InterviewTreeQuestion question)
        {
            if (disabledNodes.Contains(question.Identity))
                return;

            if (!(question.IsAnswered() && questionnaire.IsSupportFilteringForOptions(question.Identity.Id)))
                return;

            var level = GetLevel(question);
            var filter = level.GetCategoricalFilter(question.Identity);
            var checkedOptions = question.AsYesNo.GetAnswer().CheckedOptions;
            var newCheckedOptions =
                checkedOptions.Where(x => RunOptionFilter(filter, x.Value)).ToArray();

            if (newCheckedOptions.Length != checkedOptions.Count)
            {
                question.AsYesNo.SetAnswer(YesNoAnswer.FromCheckedYesNoAnswerOptions(newCheckedOptions));
                // remove rosters, implement cheaper solutions
                question.Tree.ActualizeTree();
            }
        }

        public void UpdateCascadingQuestion(InterviewTreeQuestion question)
        {
            if (disabledNodes.Contains(question.Identity))
                return;

            //move to cascading
            var cascadingParent = question.AsCascading.GetCascadingParentTreeQuestion();
            if (cascadingParent.IsDisabled() || !cascadingParent.IsAnswered())
            {
                if (question.IsAnswered())
                    question.RemoveAnswer();
                question.Disable();
            }
            else
            {
                var selectedParentValue = cascadingParent.AsSingleFixedOption.GetAnswer().SelectedValue;
                if (!questionnaire.HasAnyCascadingOptionsForSelectedParentOption(question.Identity.Id,
                    cascadingParent.Identity.Id, selectedParentValue))
                {
                    question.Disable();
                }
                else
                {
                    question.Enable();
                }
            }
        }

        public void UpdateLinkedQuestion(InterviewTreeQuestion question)
        {
            if (disabledNodes.Contains(question.Identity))
                return;

            var level = GetLevel(question);
            var optionsAndParents = question.GetCalculatedLinkedOptions();
            var options = new List<RosterVector>();
            foreach (var optionAndParent in optionsAndParents)
            {
                var optionLevel = this.expressionStorage.GetLevel(optionAndParent.ParenRoster);
                Func<IInterviewLevel, bool> filter = optionLevel.GetLinkedQuestionFilter(question.Identity);
                if (filter == null)
                {
                    options.Add(optionAndParent.Option);
                }
                else
                {
                    if (RunLinkedFilter(filter, level))
                        options.Add(optionAndParent.Option);
                }
            }
            question.UpdateLinkedOptionsAndResetAnswerIfNeeded(options.ToArray(), this.removeLinkedAnswers);
        }

        public void UpdateLinkedToListQuestion(InterviewTreeQuestion question)
        {
            if (disabledNodes.Contains(question.Identity))
                return;

            question.CalculateLinkedToListOptions(true);
        }

        public void UpdateRoster(InterviewTreeRoster roster)
        {
            if (disabledNodes.Contains(roster.Identity))
                return;

            roster.UpdateRosterTitle((questionId, answerOptionValue) => questionnaire
                .GetOptionForQuestionByOptionValue(questionId, answerOptionValue).Title);
        }

        public void UpdateVariable(InterviewTreeVariable variable)
        {
            if (disabledNodes.Contains(variable.Identity))
                return;

            var level = GetLevel(variable);

            Func<object> expression = level.GetVariableExpression(variable.Identity);
            variable.SetValue(GetVariableValue(expression));
        }

        public void UpdateValidations(InterviewTreeStaticText staticText)
        {
            IInterviewLevel level = this.GetLevel(staticText);
            var validationExpressions = level.GetValidationExpressions(staticText.Identity) ?? new Func<bool>[0];
            var validationResult = validationExpressions.Select(RunConditionExpression)
                .Select((x, i) => !x ? new FailedValidationCondition(i) : null)
                .Where(x => x != null)
                .ToArray();

            if (validationResult.Any())
                staticText.MarkInvalid(validationResult);
            else
                staticText.MarkValid();
        }

        public void UpdateValidations(InterviewTreeQuestion question)
        {
            if (!question.IsAnswered())
            {
                question.MarkValid();
                return;
            }

            IInterviewLevel level = this.GetLevel(question);
            var validationExpressions = level.GetValidationExpressions(question.Identity) ?? new Func<bool>[0];
            var validationResult = validationExpressions.Select(RunConditionExpression)
                .Select((x, i) => !x ? new FailedValidationCondition(i) : null)
                .Where(x => x != null)
                .ToArray();

            if (validationResult.Any())
                question.MarkInvalid(validationResult);
            else
                question.MarkValid();
        }

        private IInterviewLevel GetLevel(IInterviewTreeNode entity)
        {
            var nearestRoster = entity is InterviewTreeRoster
                ? entity.Identity
                : entity.Parents.OfType<InterviewTreeRoster>().LastOrDefault()?.Identity ?? this.questionnaireIdentity;

            var level = this.expressionStorage.GetLevel(nearestRoster);
            return level;
        }

        private static object GetVariableValue(Func<object> expression)
        {
            try
            {
                return expression();
            }
            catch
            {
                return null;
            }
        }

        private static bool RunLinkedFilter(Func<IInterviewLevel, bool> filter, IInterviewLevel level)
        {
            try
            {
                return filter(level);
            }
            catch
            {
                return false;
            }
        }


        private static bool RunOptionFilter(Func<int, bool> filter, int selectedValue)
        {
            try
            {
                return filter(selectedValue);
            }
            catch
            {
                return false;
            }
        }

        private static bool RunConditionExpression(Func<bool> expression)
        {
            try
            {
                return expression == null || expression();
            }
            catch
            {
                return false;
            }
        }
    }
}