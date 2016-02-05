﻿using System;
using System.Globalization;
using MvvmCross.Platform.Converters;
using WB.Core.BoundedContexts.Interviewer.Views;


namespace WB.UI.Interviewer.Converters
{
    public class IsSynchronizationFailOrCanceledConverter : MvxValueConverter<SynchronizationStatus, bool>
    {
        protected override bool Convert(SynchronizationStatus status, Type targetType, object parameter, CultureInfo culture)
        {
            return status == SynchronizationStatus.Fail || status == SynchronizationStatus.Canceled;
        }
    }
}