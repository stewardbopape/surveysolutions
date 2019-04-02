﻿using System;
using System.Collections.Generic;
using System.Linq;
using WB.Core.BoundedContexts.Headquarters.AssignmentImport;
using WB.Core.BoundedContexts.Headquarters.Assignments;
using WB.Core.BoundedContexts.Headquarters.Commands;
using WB.Core.BoundedContexts.Headquarters.Invitations;
using WB.Core.BoundedContexts.Headquarters.Questionnaires.Jobs;
using WB.Core.BoundedContexts.Headquarters.Services;
using WB.Core.BoundedContexts.Headquarters.Services.DeleteQuestionnaireTemplate;
using WB.Core.BoundedContexts.Headquarters.UserPreloading.Dto;
using WB.Core.BoundedContexts.Headquarters.Views.Interview;
using WB.Core.BoundedContexts.Headquarters.Views.Questionnaire;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.Infrastructure.CommandBus;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.SharedKernels.DataCollection.Commands.Interview;
using WB.Core.SharedKernels.DataCollection.Implementation.Entities;
using WB.Core.SharedKernels.DataCollection.Repositories;
using WB.Enumerator.Native.Questionnaire;

namespace WB.Core.BoundedContexts.Headquarters.Implementation.Services.DeleteQuestionnaireTemplate
{
    internal class DeleteQuestionnaireService : IDeleteQuestionnaireService
    {
        private readonly IInterviewsToDeleteFactory interviewsToDeleteFactory;
        private readonly IPlainStorageAccessor<QuestionnaireBrowseItem> questionnaireBrowseItemReader;
        private readonly ICommandService commandService;
        private readonly ILogger logger;
        private readonly ITranslationManagementService translations;
        private readonly IAssignmentsImportService importService;
        private readonly IAuditLog auditLog;

        private static readonly object DeleteInProcessLockObject = new object();
        private static readonly HashSet<string> DeleteInProcess = new HashSet<string>();
        private readonly IAssignmetnsDeletionService assignmetnsDeletionService;
        private readonly IInvitationsDeletionService invitationsDeletionService;
        private readonly IPlainKeyValueStorage<QuestionnaireLookupTable> lookupTablesStorage;
        private readonly IQuestionnaireStorage questionnaireStorage;
        private readonly DeleteQuestionnaireJobScheduler deleteQuestionnaireTask;

        public DeleteQuestionnaireService(IInterviewsToDeleteFactory interviewsToDeleteFactory, 
            ICommandService commandService,
            ILogger logger, 
            ITranslationManagementService translations,
            IAssignmentsImportService importService,
            IAuditLog auditLog,
            IPlainStorageAccessor<QuestionnaireBrowseItem> questionnaireBrowseItemReader,
            IAssignmetnsDeletionService assignmetnsDeletionService,
            IPlainKeyValueStorage<QuestionnaireLookupTable> lookupTablesStorage,
            IQuestionnaireStorage questionnaireStorage,
            DeleteQuestionnaireJobScheduler deleteQuestionnaireTask,
            IInvitationsDeletionService invitationsDeletionService)
        {
            this.interviewsToDeleteFactory = interviewsToDeleteFactory;
            this.commandService = commandService;
            this.logger = logger;
            this.translations = translations;
            this.importService = importService;
            this.auditLog = auditLog;
            this.questionnaireBrowseItemReader = questionnaireBrowseItemReader;
            this.assignmetnsDeletionService = assignmetnsDeletionService;
            this.lookupTablesStorage = lookupTablesStorage;
            this.questionnaireStorage = questionnaireStorage;
            this.deleteQuestionnaireTask = deleteQuestionnaireTask;
            this.invitationsDeletionService = invitationsDeletionService;
        }

        public void DisableQuestionnaire(Guid questionnaireId, long questionnaireVersion, Guid? userId)
        {
            this.logger.Warn($"Questionnaire {questionnaireId}${questionnaireVersion} deletion was triggered by {userId} user");
            var questionnaireIdentity = new QuestionnaireIdentity(questionnaireId, questionnaireVersion);
            var questionnaire = this.questionnaireBrowseItemReader.GetById(questionnaireIdentity.ToString());

            if (questionnaire != null)
            {
                this.auditLog.QuestionnaireDeleted(questionnaire.Title, questionnaireIdentity);

                if (!questionnaire.Disabled)
                {
                    this.commandService.Execute(new DisableQuestionnaire(questionnaireId, questionnaireVersion,
                        userId));
                    this.deleteQuestionnaireTask.Run(0);
                }
            }
        }

        public void DeleteInterviewsAndQuestionnaireAfter(Guid questionnaireId, long questionnaireVersion, Guid? userId)
        {
            var questionnaireKey = ObjectExtensions.AsCompositeKey(questionnaireId.FormatGuid(), questionnaireVersion);
             
            lock (DeleteInProcessLockObject)
            {
                if (DeleteInProcess.Contains(questionnaireKey))
                    return;

                DeleteInProcess.Add(questionnaireKey);
            }
            try
            {
                var questionnaireIdentity = new QuestionnaireIdentity(questionnaireId, questionnaireVersion);

                this.DeleteInterviews(questionnaireId, questionnaireVersion, userId);
                this.DeleteTranslations(questionnaireId, questionnaireVersion);
                this.DeleteLookupTables(questionnaireIdentity);

                var assignmentsImportStatus = this.importService.GetImportStatus();

                var isAssignmentImportIsGoing = assignmentsImportStatus?.ProcessStatus == AssignmentsImportProcessStatus.Verification ||
                                                assignmentsImportStatus?.ProcessStatus == AssignmentsImportProcessStatus.Import;

                if (!isAssignmentImportIsGoing)
                {
                    this.DeleteInvitations(questionnaireIdentity);
                    this.DeleteAssignments(questionnaireIdentity);
                    this.commandService.Execute(new DeleteQuestionnaire(questionnaireId, questionnaireVersion, userId));
                }
            }
            catch (Exception e)
            {
                this.logger.Error(e.Message, e);
            }

            lock (DeleteInProcessLockObject)
            {
                DeleteInProcess.Remove(questionnaireKey);
            }
        }

        private void DeleteInvitations(QuestionnaireIdentity questionnaireIdentity)
        {
            invitationsDeletionService.Delete(questionnaireIdentity);
        }

        private void DeleteAssignments(QuestionnaireIdentity questionnaireIdentity)
        {
            assignmetnsDeletionService.Delete(questionnaireIdentity);
        }

        private void DeleteLookupTables(QuestionnaireIdentity questionnaireIdentity)
        {
            var questionnaireDocument = questionnaireStorage.GetQuestionnaireDocument(questionnaireIdentity);
            
            foreach (var lookupTableInfo in questionnaireDocument.LookupTables)
            {
                var id = lookupTablesStorage.GetLookupKey(questionnaireIdentity, lookupTableInfo.Key);
                lookupTablesStorage.Remove(id);
            }
        }

        private void DeleteTranslations(Guid questionnaireId, long questionnaireVersion)
        {
            this.translations.Delete(new QuestionnaireIdentity(questionnaireId, questionnaireVersion));
        }

        private void DeleteInterviews(Guid questionnaireId, long questionnaireVersion, Guid? userId)
        {
            var exceptionsDuringDelete = new List<Exception>();

            //IInterviewsToDeleteFactory toDeleteFactory = this.interviewsToDeleteFactory.Invoke();
            List<InterviewSummary> listOfInterviews = this.interviewsToDeleteFactory.Load(questionnaireId, questionnaireVersion);
            do
            {
                try
                {
                    foreach (var interviewSummary in listOfInterviews)
                    {
                        this.commandService.Execute(new HardDeleteInterview(interviewSummary.InterviewId,
                            userId ?? interviewSummary.ResponsibleId));
                    }
                }
                catch (Exception e)
                {
                    this.logger.Error(e.Message, e);
                    exceptionsDuringDelete.Add(e);
                }

                listOfInterviews = this.interviewsToDeleteFactory.Load(questionnaireId, questionnaireVersion);

            } while (
                exceptionsDuringDelete.Count == 0 && 
                listOfInterviews.Any());

            if(exceptionsDuringDelete.Count>0)
                throw new AggregateException(
                    $"interview delete process failed for questionnaire {questionnaireId.FormatGuid()} v. {questionnaireVersion}", exceptionsDuringDelete);
        }
    }
}
