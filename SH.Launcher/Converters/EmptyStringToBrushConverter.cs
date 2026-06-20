using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using SH.Framework.Extensions;
using System;
using System.Globalization;

namespace SH.Launcher.Converters;

public sealed class EmptyStringToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        (value as string).IsNullOrWhiteSpace() ? Brushes.Gold : Brushes.LightCyan;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        BindingOperations.DoNothing;
}
