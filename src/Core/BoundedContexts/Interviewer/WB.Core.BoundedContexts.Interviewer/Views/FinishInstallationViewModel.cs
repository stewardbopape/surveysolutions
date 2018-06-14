using System;
using System.Threading;
using System.Threading.Tasks;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using Newtonsoft.Json;
using WB.Core.BoundedContexts.Interviewer.Implementation.Services;
using WB.Core.BoundedContexts.Interviewer.Properties;
using WB.Core.BoundedContexts.Interviewer.Services;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.GenericSubdomains.Portable.Implementation;
using WB.Core.GenericSubdomains.Portable.Services;
using WB.Core.SharedKernels.DataCollection.ValueObjects;
using WB.Core.SharedKernels.DataCollection.WebApi;
using WB.Core.SharedKernels.Enumerator.Services;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure.Storage;
using WB.Core.SharedKernels.Enumerator.ViewModels;

namespace WB.Core.BoundedContexts.Interviewer.Views
{
    public class FinishInstallationViewModel : BaseViewModel<FinishInstallationViewModelArg>
    {
        private readonly IViewModelNavigationService viewModelNavigationService;
        private readonly IPasswordHasher passwordHasher;
        private readonly IPlainStorage<InterviewerIdentity> interviewersPlainStorage;
        private readonly IInterviewerSettings interviewerSettings;
        private readonly ISynchronizationService synchronizationService;
        private readonly ILogger logger;
        private CancellationTokenSource cancellationTokenSource;
        private readonly IUserInteractionService userInteractionService;
        private const string StateKey = "interviewerIdentity";
        private readonly IQRBarcodeScanService qrBarcodeScanService;

        public FinishInstallationViewModel(
            IViewModelNavigationService viewModelNavigationService,
            IPrincipal principal,
            IPasswordHasher passwordHasher,
            IPlainStorage<InterviewerIdentity> interviewersPlainStorage,
            IInterviewerSettings interviewerSettings,
            ISynchronizationService synchronizationService,
            IQRBarcodeScanService qrBarcodeScanService,
            ILogger logger, 
            IUserInteractionService userInteractionService) : base(principal, viewModelNavigationService)
        {
            this.viewModelNavigationService = viewModelNavigationService;
            this.passwordHasher = passwordHasher;
            this.interviewersPlainStorage = interviewersPlainStorage;
            this.interviewerSettings = interviewerSettings;
            this.synchronizationService = synchronizationService;
            this.logger = logger;
            this.userInteractionService = userInteractionService;
            this.qrBarcodeScanService = qrBarcodeScanService;
        }

        protected override bool IsAuthenticationRequired => false;

        private string endpoint;
        public string Endpoint
        {
            get => this.endpoint;
            set { this.endpoint = value; RaisePropertyChanged(); }
        }

        private string userName;
        public string UserName
        {
            get => this.userName;
            set { this.userName = value; RaisePropertyChanged(); }
        }

        private string password;
        public string Password
        {
            get => this.password;
            set { this.password = value; RaisePropertyChanged(); }
        }

        private bool isEndpointValid;
        public bool IsEndpointValid
        {
            get => this.isEndpointValid;
            set { this.isEndpointValid = value; RaisePropertyChanged(); }
        }

        private bool isUserValid;
        public bool IsUserValid
        {
            get => this.isUserValid;
            set { this.isUserValid = value; RaisePropertyChanged(); }
        }

        private string errorMessage;
        public string ErrorMessage
        {
            get => this.errorMessage;
            set { this.errorMessage = value; RaisePropertyChanged(); }
        }

        private bool isInProgress;
        public bool IsInProgress
        {
            get => this.isInProgress;
            set { this.isInProgress = value; RaisePropertyChanged(); }
        }

        private IMvxAsyncCommand signInCommand;
        public IMvxAsyncCommand SignInCommand
        {
            get { return this.signInCommand ?? (this.signInCommand = new MvxAsyncCommand(this.SignInAsync, () => !IsInProgress)); }
        }

        private IMvxAsyncCommand scanAsyncCommand;
        public IMvxAsyncCommand ScanCommand
        {
            get { return this.scanAsyncCommand ?? (this.scanAsyncCommand = new MvxAsyncCommand(this.ScanAsync, () => !IsInProgress)); }
        }

        private async Task ScanAsync()
        {
            this.IsInProgress = true;

            try
            {
                var scanCode = await this.qrBarcodeScanService.ScanAsync();

                if (scanCode != null)
                {
                    if (Uri.TryCreate(scanCode.Code, UriKind.Absolute, out var uriResult))
                    {
                        var seachTerm = "/api/interviewersync";
                        var position = scanCode.Code.IndexOf(seachTerm, StringComparison.InvariantCultureIgnoreCase);

                        this.Endpoint = position > 0 ? scanCode.Code.Substring(0, position) : scanCode.Code;
                    }
                    else
                    {
                        var finishInfo = JsonConvert.DeserializeObject<FinishInstallationInfo>(scanCode.Code);

                        this.Endpoint = finishInfo.Url;
                        this.UserName = finishInfo.Login;
                        this.Password = finishInfo.Password;
                    }
                }
            }
            catch
            {
            }
            finally
            {
                this.IsInProgress = false;
            }
        }

        public IMvxAsyncCommand NavigateToDiagnosticsPageCommand => new MvxAsyncCommand(this.viewModelNavigationService.NavigateToAsync<DiagnosticsViewModel>);

        public override void Prepare(FinishInstallationViewModelArg parameter)
        {
            this.UserName = parameter.UserName;
        }

        public override Task Initialize()
        {
            this.IsUserValid = true;
            this.IsEndpointValid = true;
            this.Endpoint =  this.interviewerSettings.Endpoint;

#if DEBUG
            this.Endpoint = "http://192.168.88./headquarters";
            this.UserName = "int";
            this.Password = "1";
#endif
            return Task.CompletedTask;
        }

        protected override void SaveStateToBundle(IMvxBundle bundle)
        {
            base.SaveStateToBundle(bundle);
            if (this.UserName != null)
            {
                bundle.Data[StateKey] = this.userName;
            }
        }

        protected override void ReloadFromBundle(IMvxBundle state)
        {
            base.ReloadFromBundle(state);
            if (state.Data.ContainsKey(StateKey))
            {
                this.UserName = state.Data[StateKey];
            }
        }

        public async Task RefreshEndpoint()
        {
            var settingsEndpoint = this.interviewerSettings.Endpoint;
            if (!string.IsNullOrEmpty(settingsEndpoint) && !string.Equals(settingsEndpoint, this.endpoint, StringComparison.OrdinalIgnoreCase))
            {
                var message = string.Format(InterviewerUIResources.FinishInstallation_EndpointDiffers,  this.Endpoint, settingsEndpoint);
                if (await this.userInteractionService.ConfirmAsync(message, isHtml: false).ConfigureAwait(false))
                {
                    this.Endpoint = settingsEndpoint;
                }
            }
        }

        private async Task SignInAsync()
        {
            this.IsUserValid = true;
            this.IsEndpointValid = true;
            bool isNeedNavigateToRelinkPage = false;

            if (this.Endpoint?.StartsWith("@") == true)
            {
                this.Endpoint = $"https://{this.Endpoint.Substring(1)}.mysurvey.solutions";
            }

            this.interviewerSettings.SetEndpoint(this.Endpoint);

            var restCredentials = new RestCredentials
            {
                Login = this.userName
            };

            cancellationTokenSource = new CancellationTokenSource();
            this.IsInProgress = true;
            InterviewerIdentity interviewerIdentity = null;
            try
            {
                if (string.IsNullOrWhiteSpace(UserName))
                {
                    throw new SynchronizationException(SynchronizationExceptionType.Unauthorized, InterviewerUIResources.Login_WrongPassword);
                }

                var authToken = await this.synchronizationService.LoginAsync(new LogonInfo
                {
                    Username = this.UserName,
                    Password = this.Password
                }, restCredentials, cancellationTokenSource.Token).ConfigureAwait(false);

                restCredentials.Token = authToken;

                var interviewer = await this.synchronizationService.GetInterviewerAsync(restCredentials, token: cancellationTokenSource.Token).ConfigureAwait(false);

                interviewerIdentity = new InterviewerIdentity
                {
                    Id = interviewer.Id.FormatGuid(),
                    UserId = interviewer.Id,
                    SupervisorId = interviewer.SupervisorId,
                    Name = this.UserName,
                    PasswordHash = this.passwordHasher.Hash(this.Password),
                    Token = restCredentials.Token
                };

                if (!await this.synchronizationService.HasCurrentInterviewerDeviceAsync(credentials: restCredentials, token: cancellationTokenSource.Token).ConfigureAwait(false))
                {
                    await this.synchronizationService.LinkCurrentInterviewerToDeviceAsync(credentials: restCredentials, token: cancellationTokenSource.Token).ConfigureAwait(false);
                }

                await this.synchronizationService.CanSynchronizeAsync(credentials: restCredentials, token: cancellationTokenSource.Token).ConfigureAwait(false);
                
                this.interviewersPlainStorage.Store(interviewerIdentity);

                this.Principal.SignIn(restCredentials.Login, this.Password, true);
                await this.viewModelNavigationService.NavigateToDashboardAsync();
            }
            catch (SynchronizationException ex)
            {
                switch (ex.Type)
                {
                    case SynchronizationExceptionType.HostUnreachable:
                    case SynchronizationExceptionType.InvalidUrl:
                    case SynchronizationExceptionType.ServiceUnavailable:
                        this.IsEndpointValid = false;
                        break;
                    case SynchronizationExceptionType.UserIsNotInterviewer:
                    case SynchronizationExceptionType.UserLocked:
                    case SynchronizationExceptionType.UserNotApproved:
                    case SynchronizationExceptionType.Unauthorized:
                        this.IsUserValid = false;
                        break;
                    case SynchronizationExceptionType.UserLinkedToAnotherDevice:
                        isNeedNavigateToRelinkPage = true;
                        break;
                }
                this.ErrorMessage = ex.Message;
            }
            catch (Exception ex)
            {
                this.ErrorMessage = InterviewerUIResources.UnexpectedException;
                this.logger.Error("Login view model. Unexpected exception", ex);
            }
            finally
            {
                this.IsInProgress = false;
                cancellationTokenSource = null;
            }

            if (isNeedNavigateToRelinkPage)
                await this.viewModelNavigationService.NavigateToAsync<RelinkDeviceViewModel, RelinkDeviceViewModelArg>(new RelinkDeviceViewModelArg{ Identity = interviewerIdentity});
        }

        public void CancellInProgressTask()
        {
            this.cancellationTokenSource?.Cancel();
        }
    }
}
