﻿using System;
using System.Collections.Generic;
using System.Linq;
using Main.Core.Entities.SubEntities;
using WB.Core.SharedKernels.DataCollection.Aggregates;
using WB.Core.SharedKernels.DataCollection.Exceptions;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates.InterviewEntities;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates.InterviewEntities.Answers;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates.Invariants;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;

namespace WB.Core.SharedKernels.DataCollection.Implementation.Aggregates
{
    public partial class Interview
    {
        private void ValidatePrefilledQuestions(InterviewTree tree, IQuestionnaire questionnaire, Dictionary<Guid, AbstractAnswer> answersToFeaturedQuestions, RosterVector rosterVector = null, bool applyStrongChecks = true)
        {
            var currentRosterVector = rosterVector ?? (decimal[])RosterVector.Empty;
            foreach (KeyValuePair<Guid, AbstractAnswer> answerToFeaturedQuestion in answersToFeaturedQuestions)
            {
                Guid questionId = answerToFeaturedQuestion.Key;
                AbstractAnswer answer = answerToFeaturedQuestion.Value;

                var answeredQuestion = new Identity(questionId, currentRosterVector);

                QuestionType questionType = questionnaire.GetQuestionType(questionId);

                switch (questionType)
                {
                    case QuestionType.Text:
                        this.CheckTextQuestionInvariants(questionId, currentRosterVector, questionnaire, answeredQuestion, tree, applyStrongChecks);
                        break;

                    case QuestionType.Numeric:
                        if (questionnaire.IsQuestionInteger(questionId))
                            this.CheckNumericIntegerQuestionInvariants(questionId, currentRosterVector, ((NumericIntegerAnswer)answer).Value, questionnaire,
                                answeredQuestion, tree, applyStrongChecks);
                        else
                            this.CheckNumericRealQuestionInvariants(questionId, currentRosterVector, ((NumericRealAnswer)answer).Value, questionnaire,
                                answeredQuestion, tree, applyStrongChecks);
                        break;

                    case QuestionType.DateTime:
                        this.CheckDateTimeQuestionInvariants(questionId, currentRosterVector, questionnaire, answeredQuestion, tree, applyStrongChecks);
                        break;

                    case QuestionType.SingleOption:
                        this.CheckSingleOptionQuestionInvariants(questionId, currentRosterVector, ((CategoricalFixedSingleOptionAnswer)answer).SelectedValue, questionnaire,
                            answeredQuestion, tree, false ,applyStrongChecks);
                        break;

                    case QuestionType.MultyOption:
                        if (questionnaire.IsQuestionYesNo(questionId))
                        {
                            this.CheckYesNoQuestionInvariants(new Identity(questionId, currentRosterVector), (YesNoAnswer) answer, questionnaire, tree, applyStrongChecks);
                        }
                        else
                        {
                            this.CheckMultipleOptionQuestionInvariants(questionId, currentRosterVector, ((CategoricalFixedMultiOptionAnswer)answer).CheckedValues, questionnaire, answeredQuestion, tree, applyStrongChecks);
                        }
                        break;
                    case QuestionType.QRBarcode:
                        this.CheckQRBarcodeInvariants(questionId, currentRosterVector, questionnaire, answeredQuestion, tree, applyStrongChecks);
                        break;
                    case QuestionType.GpsCoordinates:
                        this.CheckGpsCoordinatesInvariants(questionId, currentRosterVector, questionnaire, answeredQuestion, tree, applyStrongChecks);
                        break;
                    case QuestionType.TextList:
                        this.CheckTextListInvariants(questionId, currentRosterVector, questionnaire, answeredQuestion, ((TextListAnswer)answer).ToTupleArray(), tree, applyStrongChecks);
                        break;

                    default:
                        throw new InterviewException(
                            $"Question {questionId} has type {questionType} which is not supported as initial pre-filled question. InterviewId: {this.EventSourceId}");
                }
            }
        }

        private void CheckLinkedMultiOptionQuestionInvariants(Guid questionId, RosterVector rosterVector,
            decimal[][] linkedQuestionSelectedOptions, IQuestionnaire questionnaire, Identity answeredQuestion,
            InterviewTree tree, bool applyStrongChecks = true)
        {
            var treeInvariants = new InterviewTreeInvariants(tree);
            var questionInvariants = new InterviewQuestionInvariants(this.properties.Id, questionId, questionnaire);

            questionInvariants.RequireQuestionExists();
            questionInvariants.RequireQuestionType(QuestionType.MultyOption);

            if (applyStrongChecks)
            {
                treeInvariants.RequireRosterVectorQuestionInstanceExists(questionId, rosterVector);
                treeInvariants.RequireQuestionIsEnabled(answeredQuestion);
            }

            if (!linkedQuestionSelectedOptions.Any())
                return;

            var linkedQuestionIdentity = new Identity(questionId, rosterVector);

            foreach (var selectedRosterVector in linkedQuestionSelectedOptions)
            {
                treeInvariants.RequireLinkedOptionIsAvailable(linkedQuestionIdentity, selectedRosterVector);
            }

            this.ThrowIfLengthOfSelectedValuesMoreThanMaxForSelectedAnswerOptions(questionId, linkedQuestionSelectedOptions.Length, questionnaire);
        }

        private void CheckLinkedSingleOptionQuestionInvariants(Guid questionId, RosterVector rosterVector, decimal[] linkedQuestionSelectedOption, IQuestionnaire questionnaire, Identity answeredQuestion, InterviewTree tree, bool applyStrongChecks = true)
        {
            var treeInvariants = new InterviewTreeInvariants(tree);
            var questionInvariants = new InterviewQuestionInvariants(this.properties.Id, questionId, questionnaire);

            questionInvariants.RequireQuestionExists();
            questionInvariants.RequireQuestionType(QuestionType.SingleOption);

            if (applyStrongChecks)
            {
                treeInvariants.RequireRosterVectorQuestionInstanceExists(questionId, rosterVector);
                treeInvariants.RequireQuestionIsEnabled(answeredQuestion);
            }

            var linkedQuestionIdentity = new Identity(questionId, rosterVector);

            treeInvariants.RequireLinkedOptionIsAvailable(linkedQuestionIdentity, linkedQuestionSelectedOption);
        }

        private void CheckNumericRealQuestionInvariants(Guid questionId, RosterVector rosterVector, double answer,
           IQuestionnaire questionnaire,
           Identity answeredQuestion, InterviewTree tree, bool applyStrongChecks = true)
        {
            var treeInvariants = new InterviewTreeInvariants(tree);
            var questionInvariants = new InterviewQuestionInvariants(this.properties.Id, questionId, questionnaire);

            questionInvariants.RequireQuestionExists();
            questionInvariants.RequireQuestionType(QuestionType.Numeric);
            questionInvariants.RequireNumericRealQuestion();

            if (applyStrongChecks)
            {
                treeInvariants.RequireRosterVectorQuestionInstanceExists(questionId, rosterVector);
                treeInvariants.RequireQuestionIsEnabled(answeredQuestion);
                this.ThrowIfAnswerHasMoreDecimalPlacesThenAccepted(questionnaire, questionId, answer);
            }
        }

        private void CheckDateTimeQuestionInvariants(Guid questionId, RosterVector rosterVector, IQuestionnaire questionnaire,
            Identity answeredQuestion, InterviewTree tree, bool applyStrongChecks = true)
        {
            var treeInvariants = new InterviewTreeInvariants(tree);
            var questionInvariants = new InterviewQuestionInvariants(this.properties.Id, questionId, questionnaire);

            questionInvariants.RequireQuestionExists();
            questionInvariants.RequireQuestionType(QuestionType.DateTime);

            if (applyStrongChecks)
            {
                treeInvariants.RequireRosterVectorQuestionInstanceExists(questionId, rosterVector);
                treeInvariants.RequireQuestionIsEnabled(answeredQuestion);
            }
        }

        private void CheckSingleOptionQuestionInvariants(Guid questionId, RosterVector rosterVector, decimal selectedValue,
            IQuestionnaire questionnaire, Identity answeredQuestion, InterviewTree tree, bool isLinkedToList,
            bool applyStrongChecks = true)
        {
            var treeInvariants = new InterviewTreeInvariants(tree);
            var questionInvariants = new InterviewQuestionInvariants(this.properties.Id, questionId, questionnaire);

            questionInvariants.RequireQuestionExists();
            questionInvariants.RequireQuestionType(QuestionType.SingleOption);

            if (isLinkedToList)
            {
                var linkedQuestionIdentity = new Identity(questionId, rosterVector);
                treeInvariants.RequireLinkedToListOptionIsAvailable(linkedQuestionIdentity, selectedValue);
            }
            else
                this.ThrowIfValueIsNotOneOfAvailableOptions(questionId, selectedValue, questionnaire);

            if (applyStrongChecks)
            {
                treeInvariants.RequireCascadingQuestionAnswerCorrespondsToParentAnswer(answeredQuestion, selectedValue, 
                    this.QuestionnaireIdentity, questionnaire.Translation);
                treeInvariants.RequireRosterVectorQuestionInstanceExists(questionId, rosterVector);
                treeInvariants.RequireQuestionIsEnabled(answeredQuestion);
            }
        }

        private void CheckMultipleOptionQuestionInvariants(Guid questionId, RosterVector rosterVector, IReadOnlyCollection<int> selectedValues,
            IQuestionnaire questionnaire, Identity answeredQuestion, InterviewTree tree, bool isLinkedToList,
            bool applyStrongChecks = true)
        {
            var treeInvariants = new InterviewTreeInvariants(tree);
            var questionInvariants = new InterviewQuestionInvariants(this.properties.Id, questionId, questionnaire);

            questionInvariants.RequireQuestionExists();
            questionInvariants.RequireQuestionType(QuestionType.MultyOption);

            if (isLinkedToList)
            {
                var linkedQuestionIdentity = new Identity(questionId, rosterVector);
                foreach (var selectedValue in selectedValues)
                {
                    treeInvariants.RequireLinkedToListOptionIsAvailable(linkedQuestionIdentity, selectedValue);
                }
            }
            else
                this.ThrowIfSomeValuesAreNotFromAvailableOptions(questionId, selectedValues, questionnaire);

            if (questionnaire.IsQuestionYesNo(questionId))
            {
                throw new InterviewException($"Question {questionId} has Yes/No type, but command is sent to Multiopions type. questionnaireId: {this.QuestionnaireId}, interviewId {this.EventSourceId}");
            }

            if (questionnaire.ShouldQuestionSpecifyRosterSize(questionId))
            {
                this.ThrowIfRosterSizeAnswerIsNegativeOrGreaterThenMaxRosterRowCount(questionId, selectedValues.Count, questionnaire);
                var maxSelectedAnswerOptions = questionnaire.GetMaxSelectedAnswerOptions(questionId);
                this.ThrowIfRosterSizeAnswerIsGreaterThenMaxRosterRowCount(questionId, selectedValues.Count,
                    questionnaire,
                    maxSelectedAnswerOptions ?? questionnaire.GetMaxRosterRowCount());
            }

            if (applyStrongChecks)
            {
                treeInvariants.RequireRosterVectorQuestionInstanceExists(questionId, rosterVector);
                this.ThrowIfLengthOfSelectedValuesMoreThanMaxForSelectedAnswerOptions(questionId, selectedValues.Count, questionnaire);
                treeInvariants.RequireQuestionIsEnabled(answeredQuestion);
            }
        }

        private void CheckYesNoQuestionInvariants(Identity question, YesNoAnswer answer, IQuestionnaire questionnaire, InterviewTree tree, bool applyStrongChecks = true)
        {
            int[] selectedValues = answer.CheckedOptions.Select(answeredOption => answeredOption.Value).ToArray();
            var yesAnswersCount = answer.CheckedOptions.Count(answeredOption => answeredOption.Yes);

            var treeInvariants = new InterviewTreeInvariants(tree);
            var questionInvariants = new InterviewQuestionInvariants(this.properties.Id, question.Id, questionnaire);

            questionInvariants.RequireQuestionExists();
            questionInvariants.RequireQuestionType(QuestionType.MultyOption);
            this.ThrowIfSomeValuesAreNotFromAvailableOptions(question.Id, selectedValues, questionnaire);

            if (questionnaire.ShouldQuestionSpecifyRosterSize(question.Id))
            {
                this.ThrowIfRosterSizeAnswerIsNegativeOrGreaterThenMaxRosterRowCount(question.Id, yesAnswersCount, questionnaire);
                var maxSelectedAnswerOptions = questionnaire.GetMaxSelectedAnswerOptions(question.Id);
                this.ThrowIfRosterSizeAnswerIsGreaterThenMaxRosterRowCount(question.Id, yesAnswersCount,
                    questionnaire,
                    maxSelectedAnswerOptions ?? questionnaire.GetMaxRosterRowCount());
            }

            this.ThrowIfLengthOfSelectedValuesMoreThanMaxForSelectedAnswerOptions(question.Id, yesAnswersCount, questionnaire);

            if (applyStrongChecks)
            {
                treeInvariants.RequireRosterVectorQuestionInstanceExists(question.Id, question.RosterVector);
                treeInvariants.RequireQuestionIsEnabled(question);
            }
        }

        private void CheckTextQuestionInvariants(Guid questionId, RosterVector rosterVector, IQuestionnaire questionnaire,
            Identity answeredQuestion, InterviewTree tree, bool applyStrongChecks = true)
        {
            var treeInvariants = new InterviewTreeInvariants(tree);
            var questionInvariants = new InterviewQuestionInvariants(this.properties.Id, questionId, questionnaire);

            questionInvariants.RequireQuestionExists();
            questionInvariants.RequireQuestionType(QuestionType.Text);

            if (applyStrongChecks)
            {
                treeInvariants.RequireRosterVectorQuestionInstanceExists(questionId, rosterVector);
                treeInvariants.RequireQuestionIsEnabled(answeredQuestion);
            }
        }

        private void CheckNumericIntegerQuestionInvariants(Guid questionId, RosterVector rosterVector, int answer, IQuestionnaire questionnaire,
            Identity answeredQuestion, InterviewTree tree, bool applyStrongChecks = true)
        {
            var treeInvariants = new InterviewTreeInvariants(tree);
            var questionInvariants = new InterviewQuestionInvariants(this.properties.Id, questionId, questionnaire);

            questionInvariants.RequireQuestionExists();
            questionInvariants.RequireQuestionType(QuestionType.Numeric);
            questionInvariants.RequireNumericIntegerQuestion();

            if (questionnaire.ShouldQuestionSpecifyRosterSize(questionId))
            {
                this.ThrowIfRosterSizeAnswerIsNegativeOrGreaterThenMaxRosterRowCount(questionId, answer, questionnaire);
                this.ThrowIfRosterSizeAnswerIsGreaterThenMaxRosterRowCount(questionId, answer, questionnaire,
                    questionnaire.IsQuestionIsRosterSizeForLongRoster(questionId)
                        ? questionnaire.GetMaxLongRosterRowCount()
                        : questionnaire.GetMaxRosterRowCount());
            }

            if (applyStrongChecks)
            {
                treeInvariants.RequireRosterVectorQuestionInstanceExists(questionId, rosterVector);
                treeInvariants.RequireQuestionIsEnabled(answeredQuestion);
            }
        }

        private void CheckTextListInvariants(Guid questionId, RosterVector rosterVector, IQuestionnaire questionnaire, Identity answeredQuestion, Tuple<decimal, string>[] answers, InterviewTree tree, bool applyStrongChecks = true)
        {
            var treeInvariants = new InterviewTreeInvariants(tree);
            var questionInvariants = new InterviewQuestionInvariants(this.properties.Id, questionId, questionnaire);

            questionInvariants.RequireQuestionExists();
            questionInvariants.RequireQuestionType(QuestionType.TextList);

            if (questionnaire.ShouldQuestionSpecifyRosterSize(questionId))
            {
                this.ThrowIfRosterSizeAnswerIsNegativeOrGreaterThenMaxRosterRowCount(questionId, answers.Length, questionnaire);
                var maxSelectedAnswerOptions = questionnaire.GetMaxSelectedAnswerOptions(questionId);
                this.ThrowIfRosterSizeAnswerIsGreaterThenMaxRosterRowCount(questionId, answers.Length,
                    questionnaire,
                    maxSelectedAnswerOptions ?? questionnaire.GetMaxRosterRowCount());
            }

            if (applyStrongChecks)
            {
                treeInvariants.RequireRosterVectorQuestionInstanceExists(questionId, rosterVector);
                treeInvariants.RequireQuestionIsEnabled(answeredQuestion);
                this.ThrowIfDecimalValuesAreNotUnique(answers, questionId, questionnaire);
                this.ThrowIfStringValueAreEmptyOrWhitespaces(answers, questionId, questionnaire);
                var maxAnswersCountLimit = questionnaire.GetListSizeForListQuestion(questionId);
                this.ThrowIfAnswersExceedsMaxAnswerCountLimit(answers, maxAnswersCountLimit, questionId, questionnaire);
            }
        }

        private void CheckGpsCoordinatesInvariants(Guid questionId, RosterVector rosterVector, IQuestionnaire questionnaire, Identity answeredQuestion, InterviewTree tree, bool applyStrongChecks = true)
        {
            var treeInvariants = new InterviewTreeInvariants(tree);
            var questionInvariants = new InterviewQuestionInvariants(this.properties.Id, questionId, questionnaire);

            questionInvariants.RequireQuestionExists();
            questionInvariants.RequireQuestionType(QuestionType.GpsCoordinates);

            if (applyStrongChecks)
            {
                treeInvariants.RequireRosterVectorQuestionInstanceExists(questionId, rosterVector);
                treeInvariants.RequireQuestionIsEnabled(answeredQuestion);
            }
        }

        private void CheckQRBarcodeInvariants(Guid questionId, RosterVector rosterVector, IQuestionnaire questionnaire,
         Identity answeredQuestion, InterviewTree tree, bool applyStrongChecks = true)
        {
            var treeInvariants = new InterviewTreeInvariants(tree);
            var questionInvariants = new InterviewQuestionInvariants(this.properties.Id, questionId, questionnaire);

            questionInvariants.RequireQuestionExists();
            questionInvariants.RequireQuestionType(QuestionType.QRBarcode);

            if (applyStrongChecks)
            {
                treeInvariants.RequireRosterVectorQuestionInstanceExists(questionId, rosterVector);
                treeInvariants.RequireQuestionIsEnabled(answeredQuestion);
            }
        }

        #region ThrowIfs

        private void ThrowIfAnswersExceedsMaxAnswerCountLimit(Tuple<decimal, string>[] answers, int? maxAnswersCountLimit,
            Guid questionId, IQuestionnaire questionnaire)
        {
            if (maxAnswersCountLimit.HasValue && answers.Length > maxAnswersCountLimit.Value)
            {
                throw new InterviewException(string.Format("Answers exceeds MaxAnswerCount limit for question {0}. InterviewId: {1}",
                    FormatQuestionForException(questionId, questionnaire), EventSourceId));
            }
        }

        private void ThrowIfStringValueAreEmptyOrWhitespaces(Tuple<decimal, string>[] answers, Guid questionId, IQuestionnaire questionnaire)
        {
            if (answers.Any(x => string.IsNullOrWhiteSpace(x.Item2)))
            {
                throw new InterviewException(string.Format("String values should be not empty or whitespaces for question {0}. InterviewId: {1}",
                    FormatQuestionForException(questionId, questionnaire), EventSourceId));
            }
        }

        private void ThrowIfDecimalValuesAreNotUnique(Tuple<decimal, string>[] answers, Guid questionId, IQuestionnaire questionnaire)
        {
            var decimals = answers.Select(x => x.Item1).Distinct().ToArray();
            if (answers.Length > decimals.Length)
            {
                throw new InterviewException(string.Format("Decimal values should be unique for question {0}. InterviewId: {1}",
                    FormatQuestionForException(questionId, questionnaire), EventSourceId));
            }
        }

        private void ThrowIfValueIsNotOneOfAvailableOptions(Guid questionId, decimal value, IQuestionnaire questionnaire)
        {
            var availableValues = questionnaire.GetOptionForQuestionByOptionValue(questionId, value);

            if (availableValues == null)
                throw new AnswerNotAcceptedException(string.Format(
                    "For question {0} was provided selected value {1} as answer. InterviewId: {2}",
                    FormatQuestionForException(questionId, questionnaire), value, EventSourceId));
        }

        private void ThrowIfSomeValuesAreNotFromAvailableOptions(Guid questionId, IReadOnlyCollection<int> values, IQuestionnaire questionnaire)
        {
            IEnumerable<decimal> availableValues = questionnaire.GetMultiSelectAnswerOptionsAsValues(questionId);

            bool someValueIsNotOneOfAvailable = values.Any(value => !availableValues.Contains(value));
            if (someValueIsNotOneOfAvailable)
                throw new AnswerNotAcceptedException(string.Format(
                    "For question {0} were provided selected values {1} as answer. But only following values are allowed: {2}. InterviewId: {3}",
                    FormatQuestionForException(questionId, questionnaire), JoinIntsWithComma(values),
                    JoinDecimalsWithComma(availableValues),
                    EventSourceId));
        }

        private void ThrowIfLengthOfSelectedValuesMoreThanMaxForSelectedAnswerOptions(Guid questionId, int answersCount, IQuestionnaire questionnaire)
        {
            int? maxSelectedOptions = questionnaire.GetMaxSelectedAnswerOptions(questionId);

            if (maxSelectedOptions.HasValue && maxSelectedOptions > 0 && answersCount > maxSelectedOptions)
                throw new AnswerNotAcceptedException(string.Format(
                    "For question {0} number of answers is greater than the maximum number of selected answers. InterviewId: {1}",
                    FormatQuestionForException(questionId, questionnaire), EventSourceId));
        }

        private void ThrowIfAnswerHasMoreDecimalPlacesThenAccepted(IQuestionnaire questionnaire, Guid questionId, double answer)
        {
            int? countOfDecimalPlacesAllowed = questionnaire.GetCountOfDecimalPlacesAllowedByQuestion(questionId);
            if (!countOfDecimalPlacesAllowed.HasValue)
                return;

            var roundedAnswer = Math.Round(answer, countOfDecimalPlacesAllowed.Value);
            if (roundedAnswer != answer)
                throw new AnswerNotAcceptedException(
                    string.Format(
                        "Answer '{0}' for question {1}  is incorrect because has more decimal places than allowed by questionnaire. Allowed amount of decimal places is {2}. InterviewId: {3}",
                        answer,
                        FormatQuestionForException(questionId, questionnaire),
                        countOfDecimalPlacesAllowed.Value,
                        EventSourceId));
        }

        private void ThrowIfRosterSizeAnswerIsNegativeOrGreaterThenMaxRosterRowCount(Guid questionId, int answer,
            IQuestionnaire questionnaire)
        {
            if (answer < 0)
                throw new AnswerNotAcceptedException(
                    $"Answer '{answer}' for question {FormatQuestionForException(questionId, questionnaire)} is incorrect because question is used as size of roster and specified answer is negative. InterviewId: {this.EventSourceId}");
        }

        private void ThrowIfRosterSizeAnswerIsGreaterThenMaxRosterRowCount(Guid questionId, int answer,
           IQuestionnaire questionnaire, int maxRosterRowCount)
        {
            if (answer > maxRosterRowCount)
            {
                var message = string.Format(
                    "Answer '{0}' for question {1} is incorrect because question is used as size of roster and specified answer is greater than {3}. InterviewId: {2}",
                    answer, FormatQuestionForException(questionId, questionnaire), this.EventSourceId, maxRosterRowCount);
                throw new AnswerNotAcceptedException(message);
            }
        }

        #endregion
    }
}
