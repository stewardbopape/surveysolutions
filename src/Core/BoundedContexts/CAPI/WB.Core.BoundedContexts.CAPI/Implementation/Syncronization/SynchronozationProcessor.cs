using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WB.Core.BoundedContexts.Capi.Implementation.Authorization;
using WB.Core.BoundedContexts.Capi.Services;
using WB.Core.GenericSubdomains.Utils.Implementation;
using WB.Core.GenericSubdomains.Utils.Services;
using WB.Core.SharedKernel.Structures.Synchronization;
using WB.Core.SharedKernels.DataCollection.Repositories;

namespace WB.Core.BoundedContexts.Capi.Implementation.Syncronization
{
    public class SynchronozationProcessor
    {
        private readonly ILogger logger;
        private readonly ISynchronizationService synchronizationService;
        private readonly IInterviewerSettings interviewerSettings;
        private CancellationToken cancellationToken;
        private CancellationTokenSource cancellationTokenSource;
        
        private readonly ICapiDataSynchronizationService dataProcessor;
        private readonly ICapiCleanUpService cleanUpExecutor;
        private readonly IInterviewSynchronizationFileStorage fileSyncRepository;
        private readonly ISyncPackageIdsStorage packageIdStorage;

        private readonly IDeviceChangingVerifier deviceChangingVerifier;

        private readonly ISyncAuthenticator authentificator;
        private SyncCredentials credentials;

        private Guid userId = Guid.Empty;

        public event EventHandler<SynchronizationEventArgs> StatusChanged;
        public event System.EventHandler ProcessFinished;
        public event System.EventHandler ProcessCanceling;
        public event EventHandler<SynchronizationCanceledEventArgs> ProcessCanceled;

        public SynchronozationProcessor(
            IDeviceChangingVerifier deviceChangingVerifier,
            ISyncAuthenticator authentificator, 
            ICapiDataSynchronizationService dataProcessor,
            ICapiCleanUpService cleanUpExecutor, 
            IInterviewSynchronizationFileStorage fileSyncRepository,
            ISyncPackageIdsStorage packageIdStorage,
            ILogger logger,
            ISynchronizationService synchronizationService,
            IInterviewerSettings interviewerSettings)
        {
            this.deviceChangingVerifier = deviceChangingVerifier;
            this.authentificator = authentificator;
            this.cleanUpExecutor = cleanUpExecutor;
            this.fileSyncRepository = fileSyncRepository;
            this.packageIdStorage = packageIdStorage;
            this.logger = logger;
            this.synchronizationService = synchronizationService;
            this.interviewerSettings = interviewerSettings;
            this.dataProcessor = dataProcessor;
        }

        public Task Run()
        {
            this.cancellationTokenSource = new CancellationTokenSource();
            this.cancellationToken = this.cancellationTokenSource.Token;

            return Task.Run(async () =>
            {
                try
                {
                    await this.Handshake();
                    await this.Push();
                    await this.Pull();

                    this.OnProcessFinished();
                }
                catch (Exception e)
                {
                    this.OnProcessCanceled(new List<Exception>() { e });
                }

            }, this.cancellationToken);
        }

        private async Task Handshake()
        {
            this.ExitIfCanceled();

            var userCredentials = this.authentificator.RequestCredentials();

            this.ExitIfCanceled();

            if (!userCredentials.HasValue)
                throw new Exception("User wasn't authenticated.");

            this.credentials = userCredentials.Value;

            this.OnStatusChanged(new SynchronizationEventArgs("Connecting...", Operation.Handshake, true));

            try
            {
                var isThisExpectedDevice = await this.synchronizationService.CheckExpectedDeviceAsync(credentials: this.credentials);

                var shouldThisDeviceBeLinkedToUser = false;
                if (!isThisExpectedDevice)
                {
                    shouldThisDeviceBeLinkedToUser = this.deviceChangingVerifier.ConfirmDeviceChanging();
                }

                HandshakePackage package = await this.synchronizationService.HandshakeAsync(credentials: this.credentials, shouldThisDeviceBeLinkedToUser: shouldThisDeviceBeLinkedToUser);
                
                userId = package.UserId;

                this.interviewerSettings.SetClientRegistrationId(package.ClientRegistrationKey);

                if (shouldThisDeviceBeLinkedToUser)
                {
                    this.cleanUpExecutor.DeleteAllInterviewsForUser(userId);
                }
            }
            catch (Exception e)
            {
                var knownHttpStatusCodes = new[] { HttpStatusCode.NotAcceptable, HttpStatusCode.InternalServerError, HttpStatusCode.Unauthorized };
                var restException = e as RestException;
                if (restException != null && !knownHttpStatusCodes.Contains(restException.StatusCode))
                    throw new RestException(string.Empty, restException.StatusCode, e);
                throw;
            }
        }

        private async Task Pull()
        {
            this.ExitIfCanceled();
            this.OnStatusChanged(new SynchronizationEventArgsWithPercent("Pulling", Operation.Pull, true, 0));

            await this.MigrateOldSyncTimestampToId();

            await this.PullUserPackages();
            await this.PullQuestionnairePackages();
            await this.PullInterviewPackages();
        }

        private async Task Push()
        {
            this.ExitIfCanceled();
            this.OnStatusChanged(new SynchronizationEventArgsWithPercent("Pushing interview data", Operation.Push, true, 0));

            var dataByChuncks = this.dataProcessor.GetItemsForPush();
            int chunksCounter = 1;
            foreach (var chunckDescription in dataByChuncks)
            {
                this.ExitIfCanceled();

                await this.synchronizationService.PushChunkAsync(this.credentials, chunckDescription.Content);

                this.fileSyncRepository.MoveInterviewsBinaryDataToSyncFolder(chunckDescription.EventSourceId);

                this.cleanUpExecutor.DeleteInterview(chunckDescription.EventSourceId);

                this.OnStatusChanged(new SynchronizationEventArgsWithPercent(string.Format("Pushing chunk {0} out of {1}", chunksCounter, dataByChuncks.Count), Operation.Push, true, (chunksCounter * 100) / dataByChuncks.Count));
                chunksCounter++;
            }

            this.OnStatusChanged(new SynchronizationEventArgsWithPercent("Pushing binary data", Operation.Push, true, 0));

            var binaryDatas = this.fileSyncRepository.GetBinaryFilesFromSyncFolder();
            int binaryDataCounter = 1;
            foreach (var binaryData in binaryDatas)
            {
                this.ExitIfCanceled();

                try
                {
                    await this.synchronizationService.PushBinaryAsync(
                        credentials: this.credentials,
                        interviewId: binaryData.InterviewId,
                        fileName: binaryData.FileName,
                        fileData: binaryData.GetData());

                    this.fileSyncRepository.RemoveBinaryDataFromSyncFolder(binaryData.InterviewId, binaryData.FileName);
                }
                catch (Exception e)
                {
                    this.logger.Error(e.Message, e);
                }

                this.OnStatusChanged(new SynchronizationEventArgsWithPercent("pushing binary data", Operation.Push, true, (binaryDataCounter * 100) / binaryDatas.Count));
                binaryDataCounter++;
            }
        }

        private async Task PullUserPackages()
        {
            var packageProcessor = new Func<SynchronizationChunkMeta, Task>(DownloadAndProcessUserPackage);
            await this.PullPackages(this.userId, "User", packageProcessor, SyncItemType.User);
        }

        private async Task PullQuestionnairePackages()
        {
            var packageProcessor = new Func<SynchronizationChunkMeta, Task>(DownloadAndProcessQuestionnirePackage);
            await this.PullPackages(Guid.Empty, "Questionnaire", packageProcessor, SyncItemType.Questionnaire);
        }

        private async Task PullInterviewPackages()
        {
            var packageProcessor = new Func<SynchronizationChunkMeta, Task>(DownloadAndProcessInterviewPackage);
            await this.PullPackages(this.userId, "Interview", packageProcessor, SyncItemType.Interview);
        }

        private async Task PullPackages(Guid currentUserId, string type, Func<SynchronizationChunkMeta, Task> packageProcessor, string packageType)
        {
            this.ExitIfCanceled();
            this.OnStatusChanged(new SynchronizationEventArgsWithPercent(
                string.Format("Pulling packages for {0}", type.ToLower()), Operation.Pull, true, 0));

            SyncItemsMetaContainer syncItemsMetaContainer = null;

            bool foundNeededPackages = false;
            int returnedBackCount = 0;

            var lastKnownPackageId = this.packageIdStorage.GetLastStoredPackageId(packageType, currentUserId);
            do
            {
                this.OnStatusChanged(new SynchronizationEventArgsWithPercent(
                    string.Format("Receiving list of packageIds for {0} to download", type.ToLower()), Operation.Pull, true, 0));

                syncItemsMetaContainer = await this.synchronizationService.GetPackageIdsToDownloadAsync(this.credentials, type, lastKnownPackageId);

                if (syncItemsMetaContainer == null)
                {
                    returnedBackCount++;
                    this.OnStatusChanged(
                        new SynchronizationEventArgsWithPercent(
                            string.Format("Last known package for {0} not found on server. Searching for previous. Tried {1} packageIdStorage",
                                type.ToLower(),
                                returnedBackCount),
                            Operation.Pull, true, 0));

                    lastKnownPackageId = this.packageIdStorage.GetChunkBeforeChunkWithId(packageType, lastKnownPackageId, currentUserId);
                    continue;
                }

                foundNeededPackages = true;
            }
            while (!foundNeededPackages);

            int progressCounter = 0;
            int chunksToDownload = syncItemsMetaContainer.SyncPackagesMeta.Count();

            foreach (SynchronizationChunkMeta chunk in syncItemsMetaContainer.SyncPackagesMeta)
            {
                if (this.cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                try
                {
                    await packageProcessor(chunk);
                }
                catch (Exception e)
                {
                    this.logger.Error(string.Format("{0} packge {1} wasn't processed", type, chunk.Id), e);
                    throw;
                }

                this.OnStatusChanged(
                    new SynchronizationEventArgsWithPercent(
                        string.Format("Pulling packages for {0}", type.ToLower()),
                        Operation.Pull,
                        true,
                        ((progressCounter++) * 100) / chunksToDownload));
            }
        }

        private async Task DownloadAndProcessUserPackage(SynchronizationChunkMeta chunk)
        {
            var package = await this.synchronizationService.RequestUserPackageAsync(credentials: this.credentials, chunkId: chunk.Id);
            this.dataProcessor.ProcessDownloadedPackage(package);
            this.packageIdStorage.Append(package.PackageId, SyncItemType.User, package.UserId, package.SortIndex);
        }

        private async Task DownloadAndProcessQuestionnirePackage(SynchronizationChunkMeta chunk)
        {
            var package = await this.synchronizationService.RequestQuestionnairePackageAsync(this.credentials, chunk.Id);
            this.dataProcessor.ProcessDownloadedPackage(package);
            this.packageIdStorage.Append(package.PackageId, SyncItemType.Questionnaire, Guid.Empty, package.SortIndex);
        }

        private async Task DownloadAndProcessInterviewPackage(SynchronizationChunkMeta chunk)
        {
            var package = await this.synchronizationService.RequestInterviewPackageAsync(this.credentials, chunk.Id);
            this.dataProcessor.ProcessDownloadedPackage(package);
            this.packageIdStorage.Append(package.PackageId, SyncItemType.Interview, package.UserId, package.SortIndex);
        }

        private async Task MigrateOldSyncTimestampToId()
        {
            string lastReceivedPackageId = this.interviewerSettings.GetLastReceivedPackageId();
            

            if (!string.IsNullOrEmpty(lastReceivedPackageId))
            {
                this.logger.Warn(string.Format("Migration of old version of sync. Last received package id: {0}", lastReceivedPackageId));

                long lastReceivedPackageIdOfLongType;
                if (!long.TryParse(lastReceivedPackageId, out lastReceivedPackageIdOfLongType))
                    return;
                this.OnStatusChanged(new SynchronizationEventArgs("Tablet had old installation. Migrating package timestamp to it's id", Operation.Pull, true));
                string lastReceivedChunkId = await this.synchronizationService.GetChunkIdByTimestampAsync(timestamp: lastReceivedPackageIdOfLongType, credentials: this.credentials);
                
                //this.packageIdStorage.Append(lastReceivedChunkId);
                
                this.interviewerSettings.SetLastReceivedPackageId(null);
            }
        }
       
        public void Cancel(Exception exception = null)
        {
            this.OnStatusChanged(new SynchronizationEventArgs("Cancelling", Operation.Pull, false));
            this.cancellationTokenSource.Cancel();
        }

        protected void OnProcessFinished()
        {
            if (this.cancellationToken.IsCancellationRequested)
                return;
            var handler = this.ProcessFinished;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        protected void OnProcessCanceling()
        {
            var handler = this.ProcessCanceling;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }
        
        protected void OnProcessCanceled(IList<Exception> exceptions)
        {
            var handler = this.ProcessCanceled;
            if (handler != null)
                handler(this, new SynchronizationCanceledEventArgs(exceptions));
        }

        protected void OnStatusChanged(SynchronizationEventArgs evt)
        {
            if (this.cancellationToken.IsCancellationRequested)
                return;
            var handler = this.StatusChanged;
            if (handler != null)
                handler(this, evt);
        }

        private void ExitIfCanceled()
        {
            var cancellationToken1 = this.cancellationTokenSource.Token;
            if (cancellationToken1.IsCancellationRequested)
                cancellationToken1.ThrowIfCancellationRequested();
        }
    }
}