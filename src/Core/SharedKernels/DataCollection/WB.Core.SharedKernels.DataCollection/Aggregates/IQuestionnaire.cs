﻿using System;
using System.Collections.Generic;
using Main.Core.Entities.SubEntities;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Questionnaire;

namespace WB.Core.SharedKernels.DataCollection.Aggregates
{
    public interface IQuestionnaire
    {
        /// <summary>
        /// Gets the current version of the instance as it is known in the event store.
        /// </summary>
        long Version { get; }

        IQuestion GetQuestionByStataCaption(string stataCaption);

        bool HasQuestion(Guid questionId);

        bool HasGroup(Guid groupId);

        QuestionType GetQuestionType(Guid questionId);

        Guid? GetQuestionLinkedQuestionId(Guid questionId);

        string GetQuestionTitle(Guid questionId);

        string GetQuestionVariableName(Guid questionId);

        string GetGroupTitle(Guid groupId);

        IEnumerable<decimal> GetAnswerOptionsAsValues(Guid questionId);

        int? GetMaxSelectedAnswerOptions(Guid questionId);

        bool IsCustomValidationDefined(Guid questionId);

        IEnumerable<QuestionIdAndVariableName> GetQuestionsInvolvedInCustomValidation(Guid questionId);

        string GetCustomValidationExpression(Guid questionId);

        IEnumerable<Guid> GetAllQuestionsWithNotEmptyValidationExpressions();

        IEnumerable<Guid> GetQuestionsWhichCustomValidationDependsOnSpecifiedQuestion(Guid questionId);

        IEnumerable<Guid> GetAllParentGroupsForQuestion(Guid questionId);

        string GetCustomEnablementConditionForQuestion(Guid questionId);

        string GetCustomEnablementConditionForGroup(Guid groupId);

        IEnumerable<QuestionIdAndVariableName> GetQuestionsInvolvedInCustomEnablementConditionOfGroup(Guid groupId);

        IEnumerable<QuestionIdAndVariableName> GetQuestionsInvolvedInCustomEnablementConditionOfQuestion(Guid questionId);

        IEnumerable<Guid> GetGroupsWhichCustomEnablementConditionDependsOnSpecifiedQuestion(Guid questionId);

        IEnumerable<Guid> GetQuestionsWhichCustomEnablementConditionDependsOnSpecifiedQuestion(Guid questionId);

        bool ShouldQuestionSpecifyRosterSize(Guid questionId);

        IEnumerable<Guid> GetGroupsPropagatedByQuestion(Guid questionId);

        int GetMaxAnswerValueForRoserSizeQuestion(Guid questionId);

        IEnumerable<Guid> GetParentRosterGroupsForQuestionStartingFromTop(Guid questionId);

        IEnumerable<Guid> GetParentRosterGroupsAndGroupItselfIfRosterStartingFromTop(Guid groupId);

        int GetRosterLevelForQuestion(Guid questionId);

        int GetRosterLevelForGroup(Guid groupId);

        IEnumerable<Guid> GetAllMandatoryQuestions();

        IEnumerable<Guid> GetAllQuestionsWithNotEmptyCustomEnablementConditions();

        IEnumerable<Guid> GetAllGroupsWithNotEmptyCustomEnablementConditions();

        bool IsRosterGroup(Guid groupId);

        IEnumerable<Guid> GetAllUnderlyingQuestions(Guid groupId);

        IEnumerable<Guid> GetGroupAndUnderlyingGroupsWithNotEmptyCustomEnablementConditions(Guid groupId);

        IEnumerable<Guid> GetUnderlyingQuestionsWithNotEmptyCustomEnablementConditions(Guid groupId);

        IEnumerable<Guid> GetUnderlyingQuestionsWithNotEmptyCustomValidationExpressions(Guid groupId);
        
        IEnumerable<Guid> GetUnderlyingMandatoryQuestions(Guid groupId);

        Guid GetQuestionReferencedByLinkedQuestion(Guid linkedQuestionId);
        
        bool IsQuestionMandatory(Guid questionId);

        bool IsQuestionInteger(Guid questionId);

        int? GetCountOfDecimalPlacesAllowedByQuestion(Guid questionId);
    }
}