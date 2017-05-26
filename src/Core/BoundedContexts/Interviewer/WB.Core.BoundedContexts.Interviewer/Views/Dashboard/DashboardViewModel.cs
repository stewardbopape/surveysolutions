using System;
using MvvmCross.Core.ViewModels;
using MvvmCross.Plugins.Messenger;
using WB.Core.BoundedContexts.Interviewer.Properties;
using WB.Core.BoundedContexts.Interviewer.Views.Dashboard.Messages;
using WB.Core.GenericSubdomains.Portable;
using WB.Core.SharedKernels.Enumerator.Services;
using WB.Core.SharedKernels.Enumerator.Services.Infrastructure;
using WB.Core.SharedKernels.Enumerator.ViewModels;
using WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails.Groups;


namespace WB.Core.BoundedContexts.Interviewer.Views.Dashboard
{
    public class DashboardViewModel : BaseViewModel, IDisposable
    {
        private readonly IViewModelNavigationService viewModelNavigationService;
        private readonly IMvxMessenger messenger;

        private MvxSubscriptionToken startingLongOperationMessageSubscriptionToken;

        public NewInterviewsViewModel NewInterviews { get; }
        public QuestionnairesViewModel Questionnaires { get; }
        public StartedInterviewsViewModel StartedInterviews { get; }
        public CompletedInterviewsViewModel CompletedInterviews { get; }
        public RejectedInterviewsViewModel RejectedInterviews { get; }

        public DashboardViewModel(IViewModelNavigationService viewModelNavigationService,
            IPrincipal principal, 
            SynchronizationViewModel synchronization,
            IMvxMessenger messenger,
            QuestionnairesViewModel questionnairesViewModel,
            StartedInterviewsViewModel startedInterviewsViewModel,
            CompletedInterviewsViewModel completedInterviewsViewModel,
            RejectedInterviewsViewModel rejectedInterviewsViewModel,
            NewInterviewsViewModel interviewsViewModel) : base(principal, viewModelNavigationService)
        {
            this.viewModelNavigationService = viewModelNavigationService;
            this.messenger = messenger;
            this.Synchronization = synchronization;
            this.Synchronization.SyncCompleted += (sender, args) => this.RefreshDashboard();

            this.Questionnaires = questionnairesViewModel;
            this.StartedInterviews = startedInterviewsViewModel;
            this.CompletedInterviews = completedInterviewsViewModel;
            this.RejectedInterviews = rejectedInterviewsViewModel;
            this.NewInterviews = interviewsViewModel;
        }

        private IMvxCommand synchronizationCommand;
        public IMvxCommand SynchronizationCommand
        {
            get
            {
                return synchronizationCommand ??
                       (synchronizationCommand = new MvxCommand(this.RunSynchronization,
                           () => !this.Synchronization.IsSynchronizationInProgress));
            }
        }
        public IMvxCommand SignOutCommand => new MvxCommand(this.SignOut);
        public IMvxCommand NavigateToDiagnosticsPageCommand => new MvxCommand(this.NavigateToDiagnostics);
        
        private bool isInProgress;
        public bool IsInProgress
        {
            get { return this.isInProgress; }
            set { this.isInProgress = value; this.RaisePropertyChanged(); }
        }

        private GroupStatus typeOfInterviews;
        public GroupStatus TypeOfInterviews
        {
            get { return this.typeOfInterviews; }
            set { this.typeOfInterviews = value; this.RaisePropertyChanged(); }
        }

        private int numberOfAssignedInterviews => this.NewInterviews.NewInterviewsCount
                                                  + this.StartedInterviews.Items.Count
                                                  + this.CompletedInterviews.Items.Count
                                                  + this.RejectedInterviews.Items.Count;

        public bool IsExistsAnyCensusQuestionnairesOrInterviews
            => numberOfAssignedInterviews + this.Questionnaires.Items.Count > 0;

        private SynchronizationViewModel synchronization;
        public SynchronizationViewModel Synchronization
        {
            get { return synchronization; }
            set
            {
                this.synchronization = value;
                this.RaisePropertyChanged();
            }
        }

        private bool isLoaded;
        public bool IsLoaded
        {
            get { return this.isLoaded; }
            set { this.isLoaded = value; this.RaisePropertyChanged(); }
        }

        public string DashboardTitle
            => InterviewerUIResources.Dashboard_Title.FormatString(this.numberOfAssignedInterviews,
                this.principal.CurrentUserIdentity.Name);

        public override void Load()
        {
            startingLongOperationMessageSubscriptionToken = this.messenger.Subscribe<StartingLongOperationMessage>(this.DashboardItemOnStartingLongOperation);

            this.Synchronization.Init();
            this.StartedInterviews.OnInterviewRemoved += this.OnInterviewRemoved;
            this.CompletedInterviews.OnInterviewRemoved += this.OnInterviewRemoved;
            this.RefreshDashboard();
        }

        private void OnInterviewRemoved(object sender, EventArgs e)
            => this.RaisePropertyChanged(() => this.DashboardTitle);

        private void RefreshDashboard()
        {
            if(this.principal.CurrentUserIdentity == null)
                return;

            this.Questionnaires.Load();
            this.StartedInterviews.Load();
            this.CompletedInterviews.Load();
            this.RejectedInterviews.Load();
            this.NewInterviews.Load(this.synchronization);
            
            this.RaisePropertyChanged(() => this.DashboardTitle);
            this.RaisePropertyChanged(() => this.IsExistsAnyCensusQuestionnairesOrInterviews);

            IsLoaded = true;
        }

        private void RunSynchronization()
        {
            if (this.viewModelNavigationService.HasPendingOperations)
            {
                this.viewModelNavigationService.ShowWaitMessage();
                return;
            }

            this.Synchronization.IsSynchronizationInProgress = true;
            this.Synchronization.Synchronize();
        }

        private void NavigateToDiagnostics()
        {
            this.Synchronization.CancelSynchronizationCommand.Execute();
            this.viewModelNavigationService.NavigateTo<DiagnosticsViewModel>();
        }

        private void SignOut()
        {
            this.Synchronization.CancelSynchronizationCommand.Execute();
            
            this.viewModelNavigationService.SignOutAndNavigateToLogin();
        }

        private void DashboardItemOnStartingLongOperation(StartingLongOperationMessage message)
        {
            IsInProgress = true;
        }

        public void Dispose()
        {
            messenger.Unsubscribe<StartingLongOperationMessage>(startingLongOperationMessageSubscriptionToken);
            this.StartedInterviews.OnInterviewRemoved -= this.OnInterviewRemoved;
            this.CompletedInterviews.OnInterviewRemoved -= this.OnInterviewRemoved;
        }
    }
}