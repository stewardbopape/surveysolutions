﻿using System;
using System.Collections.Generic;
using System.Linq;
using Main.Core.Entities.SubEntities;
using WB.Core.BoundedContexts.Designer.Aggregates;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire.Base;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire.Question;
using WB.Core.BoundedContexts.Designer.Commands.Questionnaire.Variable;
using WB.Core.SharedKernels.NonConficltingNamespace;
using WB.Core.SharedKernels.QuestionnaireEntities;

namespace WB.Tests.Unit.Designer
{
    internal static class QuestionnaireExtensions
    {
        public static void AddTextQuestion(
            this Questionnaire questionnaire,
            Guid questionId,
            Guid parentId,
            Guid responsibleId,
            string title = "title",
            string variableName = null,
            string variableLabel = null,
            bool isPreFilled = false,
            QuestionScope scope = QuestionScope.Interviewer,
            string enablementCondition =null ,
            string validationExpression = null,
            string validationMessage = null,
            string instructions= null,
            string mask = null,
            int? index = null,
            List<ValidationCondition>validationConditions = null)
        {
            questionnaire.AddDefaultTypeQuestionAdnMoveIfNeeded(new AddDefaultTypeQuestion(questionnaire.Id, questionId, parentId, title, responsibleId, index));
            var questionValidationConditions = validationConditions ?? new List<ValidationCondition>().ConcatWithOldConditionIfNotEmpty(validationExpression, validationMessage).ToList();
            
            questionnaire.UpdateTextQuestion(new UpdateTextQuestion(questionnaire.Id,
                questionId, 
                responsibleId,
                new CommonQuestionParameters()
                {
                    Title = title, VariableName = variableName, VariableLabel = variableLabel,
                    EnablementCondition = enablementCondition, Instructions = instructions
                }, 
                mask,
                scope,
                isPreFilled,
                questionValidationConditions));
        }

        public static void AddMultiOptionQuestion(
            this Questionnaire questionnaire,
            Guid questionId,
            Guid parentGroupId,
            Guid responsibleId,
            Option[] options = null,
            string title = "title",
            string variableName = null, 
            string variableLabel = null,
            QuestionScope scope = QuestionScope.Interviewer,
            string enablementCondition = null,
            string validationExpression =null,
            string validationMessage = null,
            string instructions = null,
            Guid? linkedToQuestionId = null,
            bool areAnswersOrdered = false,
            int? maxAllowedAnswers = null,
            bool yesNoView = false)
        {
            questionnaire.AddDefaultTypeQuestionAdnMoveIfNeeded(new AddDefaultTypeQuestion(Guid.NewGuid(), questionId, parentGroupId, title, responsibleId));
            questionnaire.UpdateMultiOptionQuestion(questionId, title, variableName, variableLabel, scope, enablementCondition, false, instructions, responsibleId, 
                options ?? new Option[2] {new Option() {Title = "1", Value = "1"}, new Option() {Title = "2", Value = "2"} },
                linkedToQuestionId, areAnswersOrdered, maxAllowedAnswers, yesNoView, 
                new List<ValidationCondition>(),linkedFilterExpression: null, properties: Create.QuestionProperties());
        }

        public static void AddSingleOptionQuestion(
            this Questionnaire questionnaire,
            Guid questionId,
            Guid parentGroupId,
            Guid responsibleId,
            Option[] options = null,
            string title = "title",
            string variableName = null, 
            string variableLabel = null,
            bool isPreFilled = false,
            QuestionScope scope = QuestionScope.Interviewer,
            string enablementCondition = null,
            string validationExpression = null,
            string validationMessage = null,
            string instructions = null,
            Guid? linkedToQuestionId = null,
            bool isFilteredCombobox = false,
            Guid? cascadeFromQuestionId = null)
        {
            questionnaire.AddDefaultTypeQuestionAdnMoveIfNeeded(new AddDefaultTypeQuestion(Guid.NewGuid(), questionId, parentGroupId, title, responsibleId));
            questionnaire.UpdateSingleOptionQuestion(questionId, title, variableName, variableLabel, isPreFilled, scope, enablementCondition, false, instructions, 
                responsibleId, options ?? new Option[] {new Option{Title = "one",Value = "1"},new Option{Title = "two",Value = "2"}},
                linkedToQuestionId, isFilteredCombobox, cascadeFromQuestionId, new List<ValidationCondition>(),
                linkedFilterExpression: null, properties: Create.QuestionProperties());
        }

        public static void AddNumericQuestion(
            this Questionnaire questionnaire,
            Guid questionId,
            Guid parentId,
            Guid responsibleId,
            string title = "title",
            string variableName = null, 
            string variableLabel = null,
            bool isPreFilled = false,
            QuestionScope scope = QuestionScope.Interviewer,
            string enablementCondition = null,
            string validationExpression = null,
            string validationMessage = null,
            string instructions = null,
            int? maxValue = null,
            bool isInteger = false,
            int? countOfDecimalPlaces = null)
        {
            questionnaire.AddDefaultTypeQuestionAdnMoveIfNeeded(new AddDefaultTypeQuestion(questionnaire.Id, questionId, parentId, title, responsibleId));
            questionnaire.UpdateNumericQuestion(new UpdateNumericQuestion(questionnaire.Id, questionId, responsibleId, 
                new CommonQuestionParameters()
                {
                    Title = title, VariableName = variableName, EnablementCondition = enablementCondition,
                    Instructions = instructions, VariableLabel=variableLabel
                    
                },
                isPreFilled, scope, isInteger, false, countOfDecimalPlaces, new List<ValidationCondition>()));
        }

        public static void AddGpsQuestion(
            this Questionnaire questionnaire,
            Guid questionId,
            Guid parentGroupId,
            Guid responsibleId,
            string title = "title",
            string variableName = null,
            string variableLabel = null,
            bool isPreFilled = false,
            QuestionScope scope = QuestionScope.Interviewer,
            string enablementCondition = null,
            string validationExpression = null,
            string validationMessage = null,
            string instructions = null,
            int? maxValue = null,
            bool isInteger = false,
            int? countOfDecimalPlaces = null)
        {
            questionnaire.AddDefaultTypeQuestionAdnMoveIfNeeded(new AddDefaultTypeQuestion(Guid.NewGuid(), questionId, parentGroupId, title, responsibleId));
            questionnaire.UpdateGpsCoordinatesQuestion(new UpdateGpsCoordinatesQuestion(questionnaire.Id, questionId, responsibleId, 
                new CommonQuestionParameters() { Title = title, VariableName = variableName, VariableLabel = variableLabel,Instructions = instructions, EnablementCondition = enablementCondition},
                isPreFilled, validationExpression, validationMessage, scope, new List<ValidationCondition>()));
        }


        public static void AddQRBarcodeQuestion(
            this Questionnaire questionnaire,
            Guid questionId,
            Guid parentGroupId,
            Guid responsibleId,
            string title = "title",
            string variableName = null,
            string variableLabel = null,
            bool isPreFilled = false,
            QuestionScope scope = QuestionScope.Interviewer,
            string enablementCondition = null,
            string validationExpression = null,
            string validationMessage = null,
            string instructions = null,
            int? maxValue = null,
            bool isInteger = false,
            int? countOfDecimalPlaces = null)
        {
            questionnaire.AddDefaultTypeQuestionAdnMoveIfNeeded(new AddDefaultTypeQuestion(Guid.NewGuid(), questionId, parentGroupId, title, responsibleId));
            questionnaire.UpdateQRBarcodeQuestion(new UpdateQRBarcodeQuestion(questionnaire.Id, questionId, responsibleId, new CommonQuestionParameters() { Title = title },
                validationExpression, validationMessage, scope, new List<ValidationCondition>()));
        }

        public static void AddTextListQuestion(
            this Questionnaire questionnaire,
            Guid questionId,
            Guid parentGroupId,
            Guid responsibleId,
            string title = "title",
            string variableName = null,
            string variableLabel = null,
            QuestionScope scope = QuestionScope.Interviewer,
            string enablementCondition = null,
            string validationExpression = null,
            string validationMessage = null,
            string instructions = null,
            int? maxAnswerCount = null,
            int? index = null)
        {
            questionnaire.AddDefaultTypeQuestionAdnMoveIfNeeded(new AddDefaultTypeQuestion(questionnaire.Id, questionId, parentGroupId, title, responsibleId, index));
            var validationConditions = new List<ValidationCondition>().ConcatWithOldConditionIfNotEmpty(validationExpression, validationMessage).ToList();

            questionnaire.UpdateTextListQuestion(new UpdateTextListQuestion(
                questionnaire.Id,
                questionId,
                responsibleId,
                new CommonQuestionParameters() { Title = title, VariableName = variableName, VariableLabel = variableLabel, EnablementCondition = enablementCondition },
                maxAnswerCount,
                scope,
                validationConditions));
        }

        public static void AddVariable(
            this Questionnaire questionnaire,
            Guid entityId,
            Guid parentId,
            Guid responsibleId,
            VariableType variableType = VariableType.String,
            string variableName = "variable",
            string variableExpression = null)
        {
            questionnaire.AddVariableAndMoveIfNeeded(
                new AddVariable(questionnaire.Id, 
                    entityId,
                    new VariableData(variableType, variableName, variableExpression), 
                    responsibleId, parentId));
            
        }

    }
}