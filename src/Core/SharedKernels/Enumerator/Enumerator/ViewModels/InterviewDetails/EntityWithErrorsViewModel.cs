﻿using System.Threading.Tasks;
using MvvmCross.Core.ViewModels;

namespace WB.Core.SharedKernels.Enumerator.ViewModels.InterviewDetails
{
    public class EntityWithErrorsViewModel : MvxViewModel
    {
        public void Init(NavigationIdentity entityIdentity, string errorMessage, NavigationState navigationState)
        {
            this.navigationState = navigationState;
            this.entityIdentity = entityIdentity;
            this.entityTitle = errorMessage;
        }

        private NavigationState navigationState;

        private string entityTitle;
        public string EntityTitle => this.entityTitle;

        private NavigationIdentity entityIdentity;

        private IMvxAsyncCommand navigateToSectionCommand;

        public IMvxAsyncCommand NavigateToSectionCommand
        {
            get
            {
                this.navigateToSectionCommand = this.navigateToSectionCommand ?? new MvxAsyncCommand(this.NavigateAsync);
                return this.navigateToSectionCommand;
            }
        }

        private async Task NavigateAsync()
        {
            await this.navigationState.NavigateToAsync(entityIdentity);
        }
    }
}