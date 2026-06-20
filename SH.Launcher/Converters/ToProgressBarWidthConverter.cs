using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace SH.Launcher.Converters;

public sealed class ToProgressBarCompletedWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is not int percent ? new GridLength(0, GridUnitType.Star) : new GridLength(percent, GridUnitType.Star);
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        BindingOperations.DoNothing;
}

public sealed class ToProgressBarRemainingWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is not int percent ? new GridLength(0, GridUnitType.Star) : new GridLength(100 - percent, GridUnitType.Star);
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        BindingOperations.DoNothing;
}
