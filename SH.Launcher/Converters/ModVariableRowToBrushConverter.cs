using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using SH.Launcher.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace SH.Launcher.Converters;

public class ModVariableRowToBrushConverter : IMultiValueConverter
{
    public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values?.Count != 2)
            return Brushes.LightCyan;

        if (values[0] is not ModVariableViewModel vm)
            return Brushes.LightCyan;

        return vm.GetCurrentValueForeground(vm.CurrentValue?.ToString() ?? string.Empty);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        BindingOperations.DoNothing;
}
