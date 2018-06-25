﻿using Android.Runtime;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using WB.Core.BoundedContexts.Supervisor.ViewModel;
using WB.UI.Shared.Enumerator.Activities;

namespace WB.UI.Supervisor.Activities.Dashboard
{
    [MvxFragmentPresentation(typeof(DashboardCompletedInterviewsViewModel), Resource.Id.dashboard_content,
        ActivityHostViewModelType = typeof(DashboardViewModel))]
    public class DashboardInterviewsFragment : BaseFragment<DashboardCompletedInterviewsViewModel>
    {
        protected override int ViewResourceId => Resource.Layout.dashboard_interviews;
    }
}
