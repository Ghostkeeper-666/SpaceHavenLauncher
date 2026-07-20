using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using SH.Framework.Extensions;
using SH.Framework.Logging;
using SH.Launcher.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SH.Launcher.Views;

public partial class ModPageView : UserControl
{
    public AppViewModel State => AppViewModel.State;
    public new DispatchQueue Dispatcher => AppViewModel.Dispatcher;
    public AppSettingsViewModel AppSettings => State.AppSettings;
    public PathViewModel Paths => State.Paths;
    public ILogger Log => State.Log;

    private string DisplayName => ViewModel?.Mod?.DisplayName;

    private ModPageViewModel ViewModel;
    private CancellationTokenSource CTS;
    private Task SearchTask;
    private SemaphoreSlim SearchSignal;
    private volatile string SearchText;

    public ModPageView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void RootGrid_PropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (DataContext is not ModPageViewModel vm || e?.Property?.Name != "Height")
            return;
        vm.AppSettings.ModPageSplitterHeight = (int)RootGrid.RowDefinitions[0].Height.Value;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SubscribeEvents();
        SyncColumnWidths();
        SetModPageSplitterHeight();

        DataGridColumnHeadersPresenter headersPresenter =
            VariableValuesGrid
            .GetVisualDescendants()
            .OfType<DataGridColumnHeadersPresenter>()
            .FirstOrDefault();

        if (headersPresenter == null)
            return;

        string[] headerDescriptions =
        [
            "MOD VARIABLES: \n\nThe mod uses these variables to customize your game experience \n\nYou may adjust the mod variables as you prefer, just pay attention to the variable description",
            "CHOSEN VALUE: \n\nThese are the values that you have chosen for each mod variable \n\nClick on any cell in this column to manually enter another value",
            "ORIGINAL GAME: \n\nThese values match those used in the original game \n\nIn case of new stuff, these values make the mod feel more like vanilla \n\nClick on any cell in this column to use its value as the CHOSEN VALUE",
            "SUGGESTED BY MODDER: \n\nThese are the values suggested by the mod owner \n\nClick on any cell in this column to use its value as the CHOSEN VALUE",
            "OLD MOD VERSION: \n\nThese are the values you have last used for the previous version of this same mod \n\nClick on any cell in this column to use its value as the CHOSEN VALUE",
        ];
        DataGridColumnHeader[] headers = headersPresenter.GetVisualChildren().OfType<DataGridColumnHeader>().ToArray();
        for (int i = 0; i < headers.Length && i < headerDescriptions.Length; ++i)
        {
            DataGridColumnHeader header = headers[i];
            ToolTip.SetTip(header, headerDescriptions[i]);
        }
    }

    private void SetModPageSplitterHeight()
    {
        RootGrid.RowDefinitions[0].Height = new(AppSettings.ModPageSplitterHeight);
        if (RootGrid.RowDefinitions[0].Height.Value != AppSettings.ModPageSplitterHeight && RootGrid.RowDefinitions[0].Height.Value > 0)
            AppSettings.ModPageSplitterHeight = (int)RootGrid.RowDefinitions[0].Height.Value;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        try
        {
            if (DataContext is not ModPageViewModel vm)
                return;
            ViewModel = vm;
            SearchText = string.Empty;
            vm.Mod.SetThemeBackground();
            //FormatDescription(InfoXmlDescriptionTextBlock, vm.Mod.Author, vm.Description, vm.Mod.ForegroundColor);
            SetModPageSplitterHeight();
            CTS = new CancellationTokenSource();
            StartBackgroundSaveTask(vm, CTS.Token);
            StartBackgroundSearchTask(vm, CTS.Token);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Log.Debug(ex);
        }
        base.OnAttachedToVisualTree(e);
    }

    private void StartBackgroundSearchTask(ModPageViewModel vm, CancellationToken ct)
    {
        SearchSignal = new SemaphoreSlim(0);
        SearchTask = Task.Run(() => SearchLoopAsync(ct), ct);
    }

    private async Task SearchLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await SearchSignal?.WaitAsync(ct);
                if (SearchSignal == null)
                    return;

                if (SearchText.IsNullOrWhiteSpace())
                {
                    Dispatcher.Run(
                        () => ViewModel.Variables = ViewModel.Mod.Variables);

                    continue;
                }

                string searchText = SearchText;

                await Task.Delay(333, ct);

                if (searchText != SearchText)
                    continue;

                List<ModVariableViewModel> vars = [];

                foreach (ModVariableViewModel v in ViewModel.Mod.Variables)
                {
                    ct.ThrowIfCancellationRequested();

                    if (searchText != SearchText)
                    {
                        vars = null;
                        break;
                    }

                    if (v.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                        vars.Add(v);
                }

                if (vars == null || vars.Count == 0)
                    continue;

                vars.Add(ViewModel.Mod.Variables.LastOrDefault());

                Dispatcher.Run(() => ViewModel.Variables = new(vars));
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        try
        {
            CTS?.Cancel();
            SearchSignal?.Release();
            SearchTask?.Wait();
            SearchSignal?.Dispose();
            CTS?.Dispose();
            SearchTask = null;
            SearchSignal = null;
            CTS = null;
        }
        catch (Exception ex)
        {
            Log.Debug(ex);
        }
        base.OnDetachedFromVisualTree(e);
    }


    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        // This grants a minimum space for the bottom area:
        if (change.Property.Name == "Bounds")
        {
            int max = (int)(Bounds.Height - 8 - 96 - 16);
            if (max <= 0)
                return;
            RootGrid.RowDefinitions[0].MaxHeight = max;
        }
        base.OnPropertyChanged(change);
    }

    private void SubscribeEvents()
    {
        // Subscribe to additional events here...
        // ...

        // Event for controlling the initial size of the top area:
        RootGrid.RowDefinitions[0].PropertyChanged -= RootGrid_PropertyChanged;
        RootGrid.RowDefinitions[0].PropertyChanged += RootGrid_PropertyChanged;

        // Subscribe to column resize events:
        IEnumerable<DataGridColumnHeader> headerColumns =
            VariableValuesGrid?
            .GetVisualDescendants()
            .OfType<DataGridColumnHeadersPresenter>()
            .FirstOrDefault()?
            .Children
            .OfType<DataGridColumnHeader>();

        if (headerColumns == null)
            return;

        foreach (DataGridColumnHeader headerColumn in headerColumns)
        {
            headerColumn.SizeChanged -= HeaderColumn_SizeChanged;
            headerColumn.SizeChanged += HeaderColumn_SizeChanged;
        }
    }


    private void StartBackgroundSaveTask(ModPageViewModel vm, CancellationToken ct)
    {
        _ = Task.Run(async () =>
        {
            Log.Debug($@"Background save task started for mod '{DisplayName}'");
            PeriodicTimer timer = new(TimeSpan.FromMilliseconds(100));
            try
            {
                while (await timer.WaitForNextTickAsync(ct))
                {
                    if (vm.Mod.Data.IsModified)
                        await vm.SaveModValues(true, ct);
                }
            }
            catch (OperationCanceledException)
            {
                // normal shutdown
                Log.Debug($@"Background save task stopped for mod '{DisplayName}'");
            }
        }, ct);
    }




    //public void FormatDescription(TextBlock textBlock, string author, string descriptionText, IBrush highlightForecolor)
    //{
    //    // TODO: Tell modders about this feature, remove hard-coded stuff from here.
    //    // TODO: Support for md description files instead of this thing.
    //    if (!author.StartsWith("ghostkeeper", StringComparison.OrdinalIgnoreCase))
    //    {
    //        textBlock.Text = descriptionText;
    //        return;
    //    }

    //    textBlock.Inlines.Clear();

    //    if (string.IsNullOrEmpty(descriptionText))
    //        return;

    //    string[] lines =
    //        (descriptionText?
    //        .Replace("\r", string.Empty)
    //        .TrimStart()
    //        .TrimEnd()
    //        ?? string.Empty)
    //        .Split('\n');

    //    foreach (string line in lines)
    //    {
    //        bool isHeader =
    //            line.Length >= 4 &&
    //            line.Equals(line.ToUpperInvariant(), StringComparison.InvariantCulture);

    //        IBrush brush = isHeader ? highlightForecolor : Brushes.LightCyan;
    //        FontWeight fontWeight = isHeader ? FontWeight.Bold : FontWeight.Regular;
    //        double fontSize = isHeader ? 18 : 14;

    //        Run run = new()
    //        {
    //            Text = $"{line}\n",
    //            Foreground = brush,
    //            FontWeight = fontWeight,
    //            FontSize = fontSize,
    //        };

    //        textBlock.Inlines.Add(run);
    //    }
    //}



    /// <summary>
    /// Keeps buttons grid columns aligned with data grid columns.
    /// </summary>
    private void SyncColumnWidths()
    {
        if (VariableValuesGrid == null || VariableToolbarGrid == null)
            return;

        ObservableCollection<DataGridColumn> valuesCols = VariableValuesGrid.Columns;
        if (valuesCols == null || valuesCols.Count == 0)
            return;

        ColumnDefinitions buttonsCols = VariableToolbarGrid.ColumnDefinitions;

        for (int i = 0; i < valuesCols.Count; i++)
        {
            double w = valuesCols[i].ActualWidth;
            if (w <= 0 || w is double.NaN)
                continue;

            buttonsCols[i].Width = new GridLength(w, GridUnitType.Pixel);
        }
    }

    private void HeaderColumn_SizeChanged(object sender, SizeChangedEventArgs e) =>
        SyncColumnWidths();


    /// <summary>
    /// Quick reset cell values with DELETE and BACKSPACE keys.
    /// </summary>
    private void OnDataGridKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not DataGrid grid)
            return;

        switch (e.Key)
        {
            case Key.F2:
                VariableValuesGrid.CommitEdit();
                if (grid.SelectedItem == null)
                    return;
                grid.ScrollIntoView(grid.SelectedItem, grid.Columns[1]);
                if (grid.SelectedItem != null)
                    grid.CurrentColumn = grid.Columns[1];
                grid.BeginEdit();
                e.Handled = true;
                return;

            case Key.Escape:
                VariableValuesGrid.CommitEdit();
                return;

            case Key.Left:
            case Key.Right:
                grid.ScrollIntoView(grid.SelectedItem, grid.Columns[1]);
                if (grid.SelectedItem != null)
                    grid.CurrentColumn = grid.Columns[1];
                return;

            default:
                return;
        }
    }

    /// <summary>
    /// Displays help for datagrid cell shortcuts in the status bar.
    /// </summary>
    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Debug.WriteLine(e.Source);

        if (DataContext is not ModPageViewModel vm)
            return;
        vm.State.StatusBarText = "You can press 'F2' to edit a variable value.";

        if (sender is not DataGrid grid)
            return;

        if (grid.SelectedItem is not ModVariableViewModel varVM)
            return;

        // Ensure VALUE column (index 1) is current
        if (grid.Columns.Count > 1)
        {
            grid.CurrentColumn = grid.Columns[1];
            grid.ScrollIntoView(varVM, grid.CurrentColumn);
            grid.BeginEdit();
        }
    }


    private void OnPreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
    {
        // We only care about the VALUE column
        if (e.Column is not DataGridTemplateColumn col || col.Header is not string header || header.Trim() != "VALUE")
            return;

        // EditingElement is the TextBox created for editing
        if (e.EditingElement is not TextBox tb)
            return;

        // Unsubscribe first to avoid duplicate handlers if editing again
        tb.TextChanged -= OnValueTextBoxTextChanged;
        tb.TextChanged += OnValueTextBoxTextChanged;

        tb.KeyDown -= OnEditingTextBoxKeyDown;
        tb.KeyDown += OnEditingTextBoxKeyDown;
    }

    private void OnValueTextBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox tb)
            return;
        if (tb.DataContext is not ModVariableViewModel vm)
            return;

        tb.Foreground = vm.GetCurrentValueForeground(tb.Text);

        // find parent DataGridCell and set its Foreground
        DataGridCell cell = tb.FindAncestorOfType<DataGridCell>();
        cell?.Foreground = tb.Foreground;
    }


    private void OnEditingTextBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox tb)
            return;

        DataGrid grid = tb.FindAncestorOfType<DataGrid>();
        if (grid is null)
            return;

        switch (e.Key)
        {
            case Key.Escape:
                grid.ScrollIntoView(grid.SelectedItem, grid.Columns[1]);
                e.Handled = true;
                return;

            case Key.Down:
            case Key.Enter:
            case Key.Up:
                int newIndex = grid.SelectedIndex - (e.Key == Key.Up ? 1 : -1);
                grid.CommitEdit();
                grid.CurrentColumn = grid.Columns[1];
                grid.SelectedIndex =
                    newIndex < 0 ? 0 :
                    newIndex >= (grid.ItemsSource as ObservableCollection<ModVariableViewModel>).Count ? newIndex - 1 :
                    newIndex;
                grid.BeginEdit();
                e.Handled = true;
                break;

            case Key.Right:
                grid.ScrollIntoView(grid.SelectedItem, grid.Columns[1]);
                int textLength = tb.Text?.Length ?? 0;
                if (tb.CaretIndex >= textLength)
                    e.Handled = true;
                return;

            case Key.Left:
                grid.ScrollIntoView(grid.SelectedItem, grid.Columns[1]);
                if (tb.CaretIndex <= 0)
                    e.Handled = true;
                return;

            default:
                return;
        }
    }


    private void OnCellEditEnded(object sender, DataGridCellEditEndedEventArgs e)
    {
        if (e.Column is not DataGridTemplateColumn col || col.Header is not string header || header.Trim() != "VALUE")
            return;

        if (e.Row.DataContext is not ModVariableViewModel vm)
            return;

        vm.UpdateForegrounds();
        DataGridCell cell = GetCell(e.Row, e.Column);
        cell.Foreground = vm.CurrentValueForeground;
    }

    private static DataGridCell GetCell(DataGridRow row, DataGridColumn column)
    {
        // Find the cells presenter inside this row
        DataGridCellsPresenter cellsPresenter = row
            .GetVisualDescendants()
            .OfType<DataGridCellsPresenter>()
            .FirstOrDefault();

        if (cellsPresenter is null)
            return null;

        // Find the cell whose Column matches the given column
        DataGridCell cell = cellsPresenter
            .GetVisualDescendants()
            .OfType<DataGridCell>()
            .Skip(1)
            .FirstOrDefault();

        return cell;
    }

    private void ResetSearch(object sender, PointerPressedEventArgs e) =>
        SearchTextbox.Text = string.Empty;

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox tb)
            return;
        SearchText = tb.Text;
        SearchSignal?.Release();
    }
}