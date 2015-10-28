﻿using System;
using System.Text.RegularExpressions;
using Ncqrs.Eventing.Storage;
using WB.Core.Infrastructure.FileSystem;
using WB.Core.Infrastructure.PlainStorage;
using WB.Core.Infrastructure.ReadSide.Repository.Accessors;
using WB.Core.Infrastructure.Transactions;
using WB.Core.SharedKernels.DataCollection.Views;
using WB.Core.SharedKernels.SurveyManagement.EventHandler;
using WB.Core.SharedKernels.SurveyManagement.Factories;
using WB.Core.SharedKernels.SurveyManagement.Views.DataExport;
using WB.Core.SharedKernels.SurveyManagement.Views.Interview;
using WB.Core.SharedKernels.SurveyManagement.Views.InterviewHistory;

namespace WB.Core.BoundedContexts.Headquarters.DataExport.Services
{
    public class TabularFormatDataExportService: IDataExportService
    {
        private readonly IStreamableEventStore eventStore;
        private readonly IReadSideRepositoryWriter<InterviewSummary> interviewSummaryReader;
        private readonly IReadSideRepositoryWriter<UserDocument> userReader;
        private readonly IReadSideKeyValueStorage<QuestionnaireExportStructure> questionnaireReader;
        private readonly InterviewDataExportSettings interviewDataExportSettings;
        private readonly ICsvWriterFactory csvWriterFactory;
        private readonly IFileSystemAccessor fileSystemAccessor;
        private readonly ITransactionManagerProvider transactionManagerProvider;
        private readonly IPlainTransactionManager plainTransactionManager;

        private readonly IDataExportQueue dataExportQueue;
        ITransactionManager TransactionManager
        {
            get { return transactionManagerProvider.GetTransactionManager(); }
        }

        public TabularFormatDataExportService(
            IStreamableEventStore eventStore, 
            IReadSideRepositoryWriter<InterviewSummary> interviewSummaryReader, 
            IReadSideRepositoryWriter<UserDocument> userReader, 
            IReadSideKeyValueStorage<QuestionnaireExportStructure> questionnaireReader, 
            InterviewDataExportSettings interviewDataExportSettings, 
            ICsvWriterFactory csvWriterFactory, 
            IFileSystemAccessor fileSystemAccessor, 
            ITransactionManagerProvider transactionManagerProvider, 
            IDataExportQueue dataExportQueue, IPlainTransactionManager plainTransactionManager)
        {
            this.eventStore = eventStore;
            this.interviewSummaryReader = interviewSummaryReader;
            this.userReader = userReader;
            this.questionnaireReader = questionnaireReader;
            this.interviewDataExportSettings = interviewDataExportSettings;
            this.csvWriterFactory = csvWriterFactory;
            this.fileSystemAccessor = fileSystemAccessor;
            this.transactionManagerProvider = transactionManagerProvider;
            this.dataExportQueue = dataExportQueue;
            this.plainTransactionManager = plainTransactionManager;
        }

        public void ExportData(Guid questionnaireId, long questionnaireVersion, string dataExportProcessId)
        {
            throw new NotImplementedException();
        }

        public void ExportParaData(string dataExportProcessId)
        {
            var eventCount = eventStore.CountOfAllEvents();
            int numberOfEventsToBeHandledInOrderToUpdateExportProgress = eventCount/100;
            var events = eventStore.GetAllEvents();
            var interviewHistoryWriter = new InterviewHistoryWriter(csvWriterFactory, fileSystemAccessor,
                interviewSummaryReader,
                interviewDataExportSettings);

            var interviewHistoryDenormalizer =
                new InterviewHistoryDenormalizer(interviewHistoryWriter, interviewSummaryReader, userReader, questionnaireReader,
                    interviewDataExportSettings);

            interviewHistoryWriter.Clear();
            interviewHistoryWriter.EnableCache();
            TransactionManager.ExecuteInQueryTransaction(
                () =>
                {
                    int countOfProcessedEvents = 0;
                    foreach (var committedEvent in events)
                    {
                        interviewHistoryDenormalizer.Handle(committedEvent);
                        if (countOfProcessedEvents <= eventCount)
                        {
                            if (countOfProcessedEvents%numberOfEventsToBeHandledInOrderToUpdateExportProgress == 0)
                            {
                                int intermediatePercents = countOfProcessedEvents/
                                                           numberOfEventsToBeHandledInOrderToUpdateExportProgress;
                                this.plainTransactionManager.ExecuteInPlainTransaction(
                                    () =>
                                        dataExportQueue.UpdateDataExportProgress(dataExportProcessId,
                                            intermediatePercents));
                            }
                        }
                        countOfProcessedEvents++;
                    }

                });
      
            interviewHistoryWriter.DisableCache();
        }
    }
}