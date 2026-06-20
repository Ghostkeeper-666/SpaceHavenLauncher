using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using SH.Framework.Logging;
using System;
using System.Globalization;

namespace SH.Launcher.Converters;

public sealed class LogLevelToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is not ELogLevel level ? Brushes.White : level switch
        {
            ELogLevel.Debug => Brushes.SkyBlue,
            ELogLevel.Success => Brushes.Lime,
            ELogLevel.Warn => Brushes.Yellow,
            ELogLevel.Error => new SolidColorBrush(Color.Parse("#FF3F1F")),
            ELogLevel.Info or _ => Brushes.White,
        };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        BindingOperations.DoNothing;
}
