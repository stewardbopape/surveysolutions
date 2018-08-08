﻿using WB.Core.SharedKernels.DataCollection;
using WB.Core.SharedKernels.DataCollection.Aggregates;

namespace WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Groups
{
    public interface IGroupStateCalculationStrategy
    {
        GroupStatus CalculateDetailedStatus(Identity groupIdentity, IStatefulInterview interview);
    }

    public interface IInterviewStateCalculationStrategy
    {
        SimpleGroupStatus CalculateSimpleStatus(IStatefulInterview interview);
        GroupStatus CalculateDetailedStatus(IStatefulInterview interview);
    }
}
