using System;
using System.Collections.Generic;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.WebApi;

namespace WB.Core.BoundedContexts.Headquarters.Assignments
{
    public interface IAssignmentsService
    {
        List<Assignment> GetAssignments(Guid responsibleId);

        List<Assignment> GetAssignmentsForSupervisor(Guid supervisorId);

        List<Guid> GetAllAssignmentIds(Guid responsibleId);
        
        Assignment GetAssignment(Guid id);

        List<Assignment> GetAssignmentsReadyForWebInterview(QuestionnaireIdentity questionnaireId);
        
        int GetCountOfAssignmentsReadyForWebInterview(QuestionnaireIdentity questionnaireId);

        int GetCountOfAssignments(QuestionnaireIdentity questionnaireId);
        
        AssignmentApiDocument MapAssignment(Assignment assignment);

        bool HasAssignmentWithProtectedVariables(Guid responsibleId);
        bool HasAssignmentWithAudioRecordingEnabled(Guid responsible);
        bool HasAssignmentWithAudioRecordingEnabled(QuestionnaireIdentity questionnaireIdentity);
    }
}
