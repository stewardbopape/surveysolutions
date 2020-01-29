﻿using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Quartz;
using WB.Core.BoundedContexts.Headquarters.AssignmentImport;
using WB.Core.BoundedContexts.Headquarters.Services.Preloading;
using WB.Core.BoundedContexts.Headquarters.Users.UserPreloading.Dto;
using WB.Core.BoundedContexts.Headquarters.Users.UserPreloading.Tasks;
using WB.Core.BoundedContexts.Headquarters.Views.SampleImport;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Enumerator.Native.WebInterview;

namespace WB.Core.BoundedContexts.Headquarters.Users.UserPreloading.Jobs
{
    [DisallowConcurrentExecution]
    internal class AssignmentsVerificationJob : IJob
    {
        private readonly ILogger logger;
        private readonly IAssignmentsImportService assignmentsImportService;
        private readonly AssignmentsImportTask assignmentsImportTask;
        private readonly SampleImportSettings sampleImportSettings;

        public AssignmentsVerificationJob(ILogger logger,
            IAssignmentsImportService assignmentsImportService,
            AssignmentsImportTask assignmentsImportTask,
            SampleImportSettings sampleImportSettings)
        {
            this.logger = logger;
            this.assignmentsImportService = assignmentsImportService;
            this.assignmentsImportTask = assignmentsImportTask;
            this.sampleImportSettings = sampleImportSettings;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                var importProcess = assignmentsImportService.GetImportStatus();

                if (importProcess?.ProcessStatus != AssignmentsImportProcessStatus.Verification) return;

                var allAssignmentIds = assignmentsImportService.GetAllAssignmentIdsToVerify();

                if (importProcess?.ProcessStatus != AssignmentsImportProcessStatus.Verification) return;

                this.logger.Debug("Assignments verification job: Started");

                var sw = new Stopwatch();
                sw.Start();

                Parallel.ForEach(allAssignmentIds,
                    new ParallelOptions { MaxDegreeOfParallelism = sampleImportSettings.InterviewsImportParallelTasksLimit },
                    assignmentId =>
                    {
                        InScopeExecutor.Current.Execute((serviceLocatorLocal) =>
                        {
                            var threadImportAssignmentsService = serviceLocatorLocal.GetInstance<IAssignmentsImportService>();
                            IQuestionnaireStorage threadQuestionnaireStorage = serviceLocatorLocal.GetInstance<IQuestionnaireStorage>();
                            IPreloadedDataVerifier threadImportAssignmentsVerifier = serviceLocatorLocal.GetInstance<IPreloadedDataVerifier>();

                            var assignmentToVerify = threadImportAssignmentsService.GetAssignmentById(assignmentId);
                            if (assignmentToVerify == null) return;

                            var questionnaire =
                                threadQuestionnaireStorage.GetQuestionnaire(importProcess.QuestionnaireIdentity, null);
                            if (questionnaire == null)
                            {
                                threadImportAssignmentsService.RemoveAssignmentToImport(assignmentToVerify.Id);
                                return;
                            }

                            var error = threadImportAssignmentsVerifier.VerifyWithInterviewTree(
                                    assignmentToVerify.Answers,
                                    assignmentToVerify.Interviewer ?? assignmentToVerify.Supervisor,
                                    questionnaire);

                            threadImportAssignmentsService.SetVerifiedToAssignment(assignmentToVerify.Id,
                                error?.ErrorMessage);
                        });
                    });

                InScopeExecutor.Current.Execute((serviceLocatorLocal) =>
                    serviceLocatorLocal.GetInstance<IAssignmentsImportService>()
                        .SetImportProcessStatus(AssignmentsImportProcessStatus.Import));

                await assignmentsImportTask.ScheduleRunAsync();

                sw.Stop();
                this.logger.Debug($"Assignments verification job: Finished. Elapsed time: {sw.Elapsed}");
            }
            catch (Exception ex)
            {
                this.logger.Error($"Assignments verification job: FAILED. Reason: {ex.Message} ", ex);
            }
        }
    }
}
