﻿using System;
using System.Linq;
using Main.Core.Entities.SubEntities;
using Ncqrs.Commanding.CommandExecution.Mapping.Attributes;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire.Base;

namespace WB.Core.BoundedContexts.Designer.Commands.Questionnaire.Question.SingleOption
{
    [Serializable]
    [MapsToAggregateRootMethod(typeof(Aggregates.Questionnaire), "AddSingleOptionQuestion")]
    public class AddSingleOptionQuestionCommand : AbstractAddQuestionCommand
    {
        public AddSingleOptionQuestionCommand(
            Guid questionnaireId,
            Guid questionId,
            Guid parentGroupId,
            string title,
            string variableName, string variableLabel,
            bool isMandatory,
            string enablementCondition,
            string instructions,
            Guid responsibleId,
            string validationExpression,
            string validationMessage,
            QuestionScope scope,
            bool isPreFilled,
            Option[] options,
            Guid? linkedToQuestionId,
            bool isFilteredCombobox)
            : base(
                responsibleId: responsibleId, questionnaireId: questionnaireId, questionId: questionId, title: title,
                variableName: variableName, isMandatory: isMandatory, enablementCondition: enablementCondition, instructions: instructions,
                parentGroupId: parentGroupId,variableLabel:variableLabel)
        {
            this.IsPreFilled = isPreFilled;
            this.Scope = scope;
            this.ValidationMessage = CommandUtils.SanitizeHtml(validationMessage, removeAllTags: true);
            this.ValidationExpression = validationExpression;
            if (options != null)
                options
                    .ToList()
                    .ForEach(x => x.Title = CommandUtils.SanitizeHtml(x.Title, removeAllTags: true));
            this.Options = options;
            this.LinkedToQuestionId = linkedToQuestionId;
            this.IsFilteredCombobox = isFilteredCombobox;
        }

        public bool IsFilteredCombobox { get; set; }

        public QuestionScope Scope { get; set; }

        public string ValidationMessage { get; set; }

        public string ValidationExpression { get; set; }

        public bool IsPreFilled { get; set; }

        public Guid? LinkedToQuestionId { get; set; }

        public Option[] Options { get; set; }
    }
}
