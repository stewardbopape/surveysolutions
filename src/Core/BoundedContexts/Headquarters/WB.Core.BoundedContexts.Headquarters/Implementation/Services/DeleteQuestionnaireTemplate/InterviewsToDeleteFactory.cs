﻿using System;
using System.Collections.Generic;
using System.Linq;
using WB.Core.BoundedContexts.Headquarters.Services.DeleteQuestionnaireTemplate;
using WB.Core.BoundedContexts.Headquarters.Views.Interview;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;

namespace WB.Core.BoundedContexts.Headquarters.Implementation.Services.DeleteQuestionnaireTemplate
{
    internal class InterviewsToDeleteFactory : IInterviewsToDeleteFactory
    {
        private readonly IQueryableReadSideRepositoryReader<InterviewSummary> interviewsReader;

        public const int BatchSize = 100;

        public InterviewsToDeleteFactory(IQueryableReadSideRepositoryReader<InterviewSummary> interviewsReader)
        {
            this.interviewsReader = interviewsReader;
        }

        public List<InterviewSummary> LoadBatch(Guid questionnaireId, long questionnaireVersion)
        {
            var result = this.interviewsReader.Query(_ => _.Where(interview => 
                interview.QuestionnaireId == questionnaireId &&
                interview.QuestionnaireVersion == questionnaireVersion)
                .Take(BatchSize)
                .ToList());

            return result;
        }
    }
}
