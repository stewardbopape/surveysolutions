using System;
using System.Collections.Generic;
using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Events.Interview.Dtos;

namespace WB.Core.BoundedContexts.Headquarters.Views.Interview
{
    public interface IInterviewFactory
    {
        Identity[] GetFlaggedQuestionIds(Guid interviewId);
        void SetFlagToQuestion(Guid interviewId, Identity questionIdentity);
        void RemoveFlagFromQuestion(Guid interviewId, Identity questionIdentity);
        void RemoveInterview(Guid interviewId);

        void UpdateAnswer(Guid interviewId, Identity questionIdentity, object answer);
        void MakeEntitiesValid(Guid interviewId, Identity[] entityIds);
        void MakeEntitiesInvalid(Guid interviewId, IReadOnlyDictionary<Identity, IReadOnlyList<FailedValidationCondition>> entityIds);
        void EnableEntities(Guid interviewId, Identity[] entityIds);
        void DisableEntities(Guid interviewId, Identity[] entityIds);
        void UpdateVariables(Guid interviewId, ChangedVariable[] variables);
        void MarkQuestionsAsReadOnly(Guid interviewId, Identity[] questionIds);
        void AddRosters(Guid interviewId, Identity[] rosterIds);
        void RemoveRosters(Guid interviewId, Identity[] rosterIds);
        void RemoveAnswers(Guid interviewId, Identity[] questionIds);
    }
}