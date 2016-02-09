﻿using System;
using System.Collections.Generic;
using Main.Core.Entities.SubEntities;
using WB.Core.SharedKernels.QuestionnaireEntities;

namespace Main.Core.Events.Questionnaire
{
    public class QuestionChanged : FullQuestionDataEvent
    {
        protected QuestionChanged() { }

        public QuestionChanged(Guid responsibleId, string conditionExpression, bool featured, string instructions, bool capital, Guid publicKey, string questionText, 
            QuestionScope questionScope, string stataExportCaption, string variableLabel, string validationExpression, string validationMessage, QuestionType questionType, 
            Order? answerOrder, Answer[] answers, Guid? groupPublicKey, Guid? linkedToQuestionId, Guid? linkedToRosterId, bool? isInteger, bool? areAnswersOrdered, bool? yesNoView, int? maxAllowedAnswers, 
            string mask, bool? isFilteredCombobox, Guid? cascadeFromQuestionId, Guid targetGroupKey, IList<ValidationCondition> validationConditions) : base(responsibleId, conditionExpression, featured, instructions, capital, publicKey, 
                questionText, questionScope, stataExportCaption, variableLabel, validationExpression, validationMessage, questionType, answerOrder, answers, groupPublicKey, 
                linkedToQuestionId, linkedToRosterId, isInteger, areAnswersOrdered, yesNoView, maxAllowedAnswers, mask, isFilteredCombobox, cascadeFromQuestionId, validationConditions)
        {
            this.TargetGroupKey = targetGroupKey;
        }

        public Guid TargetGroupKey { get; private set; }
    }
}