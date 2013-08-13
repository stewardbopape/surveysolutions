using System;
using System.Collections.Generic;
using Main.Core.Domain;
using Ncqrs.Commanding;
using Ncqrs.Commanding.CommandExecution.Mapping.Attributes;
using WB.Core.SharedKernel.Structures.Synchronization;
using WB.Core.SharedKernels.DataCollection.Commands.Interview.Base;
using WB.Core.SharedKernels.DataCollection.ValueObjects.Interview;

namespace WB.Core.SharedKernels.DataCollection.Commands.Interview
{
    [MapsToAggregateRootMethodOrConstructor(typeof(Implementation.Aggregates.Interview), "UpdateInterviewMetaInfo")]
    public class UpdateInterviewMetaInfoCommand : InterviewCommand
    {
        public UpdateInterviewMetaInfoCommand(Guid interviewId, Guid questionnarieId, Guid userId,
                                              InterviewStatus status, List<FeaturedQuestionMeta> featuredQuestionsMeta)
            : base(interviewId, userId)
        {
            Id = interviewId;
            QuestionnarieId = questionnarieId;
            InterviewStatus = status;
            FeaturedQuestionsMeta = featuredQuestionsMeta;
        }

        public Guid Id { get; set; }

        public Guid QuestionnarieId { get; set; }

        public InterviewStatus InterviewStatus { get; set; }

        public List<FeaturedQuestionMeta> FeaturedQuestionsMeta { get; set; }
    }
}
