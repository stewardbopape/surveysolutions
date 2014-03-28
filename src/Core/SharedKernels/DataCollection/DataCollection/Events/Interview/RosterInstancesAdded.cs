using System.Linq;
using WB.Core.SharedKernels.DataCollection.Events.Interview.Base;
using WB.Core.SharedKernels.DataCollection.Events.Interview.Dtos;

namespace WB.Core.SharedKernels.DataCollection.Events.Interview
{
    public class RosterInstancesAdded : InterviewPassiveEvent
    {
        public AddedRosterInstance[] Instances { get; private set; }

        public RosterInstancesAdded(AddedRosterInstance[] instances)
        {
            this.Instances = instances.ToArray();
        }
    }
}