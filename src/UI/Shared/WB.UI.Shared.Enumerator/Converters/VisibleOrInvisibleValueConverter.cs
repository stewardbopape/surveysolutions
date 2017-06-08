﻿using System;
using System.Globalization;
using Android.Views;
using MvvmCross.Platform.Converters;
using MvvmCross.Platform.Platform;

namespace WB.UI.Shared.Enumerator.Converters
{
    public class VisibleOrInvisibleValueConverter : MvxValueConverter<bool, ViewStates>
    {
        protected override ViewStates Convert(bool value, Type targetType, object parameter, CultureInfo culture)
        {
            return value ? ViewStates.Visible : ViewStates.Invisible;
        }
    }
}