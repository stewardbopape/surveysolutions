﻿using Main.Core.Commands.Questionnaire.Base;

namespace Main.Core.Commands.Questionnaire.Question
{
    using System;

    using Main.Core.Domain;
    using Main.Core.Entities.SubEntities;

    using Ncqrs.Commanding.CommandExecution.Mapping.Attributes;

    [Serializable]
    [MapsToAggregateRootMethod(typeof(QuestionnaireAR), "CloneQuestion")]
    public class CloneQuestionCommand : FullQuestionDataCommand
    {
        public CloneQuestionCommand(Guid questionnaireId, Guid questionId, Guid groupId, Guid sourceQuestionId, int targetIndex,
            string title, QuestionType type, string alias, bool isMandatory, bool isFeatured, bool isHeaderOfPropagatableGroup,
            QuestionScope scope, string condition, string validationExpression, string validationMessage, string instructions,
            Option[] options, Order optionsOrder, int? maxValue, Guid[] triggedGroupIds)
            : base(questionnaireId, questionId, title, type, alias, isMandatory, isFeatured, isHeaderOfPropagatableGroup,
                scope, condition, validationExpression, validationMessage, instructions, options, optionsOrder, maxValue, triggedGroupIds)
        {
            this.GroupId = groupId;
            this.SourceQuestionId = sourceQuestionId;
            this.TargetIndex = targetIndex;
        }

        public Guid GroupId { get; set; }
        public Guid SourceQuestionId { get; set; }
        public int TargetIndex { get; set; }
    }
}