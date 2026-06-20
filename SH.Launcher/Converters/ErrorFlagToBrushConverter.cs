using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SH.Launcher.Converters;

public sealed class ErrorFlagToBrushConverter : IMultiValueConverter
{
    public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture) =>
        values.Count != 2 ? Brushes.Magenta :
        values.Any(v => v is Avalonia.UnsetValueType || v == null) ? Brushes.Magenta :
        values[0].Equals(false) ? (IBrush)values[1] : Brushes.Tomato;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        BindingOperations.DoNothing;
}
