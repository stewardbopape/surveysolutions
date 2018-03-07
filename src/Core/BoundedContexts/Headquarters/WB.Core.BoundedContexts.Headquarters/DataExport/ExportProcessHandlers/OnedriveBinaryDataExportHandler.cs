﻿using System;
using System.IO;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.OneDrive.Sdk;
using WB.Core.BoundedContexts.Headquarters.DataExport.Accessors;
using WB.Core.BoundedContexts.Headquarters.DataExport.Services;
using WB.Core.BoundedContexts.Headquarters.Views.Interview;
using WB.Core.BoundedContexts.Headquarters.Views.InterviewHistory;
using WB.Core.Infrastructure.FileSystem;
using WB.Core.Infrastructure.Transactions;
using WB.Core.SharedKernels.DataCollection.Repositories;

namespace WB.Core.BoundedContexts.Headquarters.DataExport.ExportProcessHandlers
{
    internal class OnedriveBinaryDataExportHandler : AbstractExternalStorageDataExportHandler
    {
        public OnedriveBinaryDataExportHandler(
            IFileSystemAccessor fileSystemAccessor,
            IImageFileStorage imageFileRepository,
            IFilebasedExportedDataAccessor filebasedExportedDataAccessor,
            InterviewDataExportSettings interviewDataExportSettings,
            ITransactionManager transactionManager,
            IInterviewFactory interviewFactory,
            IDataExportProcessesService dataExportProcessesService,
            IQuestionnaireStorage questionnaireStorage,
            IDataExportFileAccessor dataExportFileAccessor,
            IAudioFileStorage audioFileStorage,
            IPlainTransactionManagerProvider plainTransactionManagerProvider)
            : base(fileSystemAccessor, filebasedExportedDataAccessor, interviewDataExportSettings,
                dataExportProcessesService, dataExportFileAccessor, questionnaireStorage, transactionManager,
                interviewFactory, imageFileRepository, audioFileStorage, plainTransactionManagerProvider)
        {
        }

        private OneDriveClient oneDriveClient;
        protected override IDisposable GetClient(string accessToken)
        {
            oneDriveClient = new OneDriveClient(
                new DelegateAuthenticationProvider(
#pragma warning disable 1998 // TODO ROMA
                    async (requestMessage) => requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken)));
#pragma warning restore 1998

            return null;
        }

        protected override string CreateApplicationFolder() => "Survey Solutions";

        protected override string CreateFolder(string applicatioFolder, string folderName) => $"{applicatioFolder}/{folderName}";

        protected override void UploadFile(string folder, byte[] fileContent, string fileName) 
            => oneDriveClient.Drive.Root.ItemWithPath($"{folder}/{fileName}").Content.Request().PutAsync<Item>(new MemoryStream(fileContent));
    }
}
