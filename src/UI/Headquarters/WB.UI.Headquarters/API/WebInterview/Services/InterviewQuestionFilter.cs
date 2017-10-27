﻿using System;
using System.Collections.Generic;
using System.Linq;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Implementation.Aggregates.InterviewEntities;

namespace WB.UI.Headquarters.API.WebInterview.Services
{
    public class InterviewQuestionFilter
    {
        private readonly Func<InterviewQuestionFilter, bool> rule;
        private readonly HashSet<Identity> flaggedQuestionsSet;
        private readonly HashSet<FilterOption> filters;
        private InterviewTreeQuestion question;

        public InterviewQuestionFilter(HashSet<Identity> flaggedQuestionsSetQuestions,
            HashSet<FilterOption> filters,
            Func<InterviewQuestionFilter, bool> rule)
        {
            this.rule = rule;
            this.flaggedQuestionsSet = flaggedQuestionsSetQuestions;
            this.filters = filters;
        }

        public bool Evaluate(InterviewTreeQuestion questionToEvaluate)
        {
            this.question = questionToEvaluate;
            return rule(this);
        }

        public bool Has(params FilterOption[] options)
        {
            return options.Any(filters.Contains);
        }

        /// <summary>
        /// Apply or rule on options if any of this options supplied for filtering
        /// </summary>
        /// <param name="options">Options to apply OR rule</param>
        public bool Or(params FilterOption[] options)
        {
            if (!Has(options)) return true;

            return options.Any(o => Is(o, false));
        }

        public bool Is(FilterOption option, bool @default = true)
        {
            if (!filters.Contains(option)) return @default;

            switch (option)
            {
                case FilterOption.Flagged: return flaggedQuestionsSet.Contains(question.Identity);
                case FilterOption.NotFlagged: return !flaggedQuestionsSet.Contains(question.Identity);
                case FilterOption.WithComments: return question.AnswerComments.Any();
                case FilterOption.Invalid: return !question.IsValid;
                case FilterOption.Valid: return question.IsValid;
                case FilterOption.Answered: return question.IsAnswered();
                case FilterOption.NotAnswered: return !question.IsAnswered();
                case FilterOption.ForSupervisor: return question.IsSupervisors;
                case FilterOption.ForInterviewer: return !question.IsSupervisors;
                default:
                    throw new ArgumentOutOfRangeException(nameof(option));
            }
        }
    }
}