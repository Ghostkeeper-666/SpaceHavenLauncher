using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SH.Modding;
using System;
using System.ComponentModel;

namespace SH.Launcher.ViewModels;

public partial class ModVariableViewModel : ObservableObject
{
    public ModViewModel Mod { get; }
    public VarData Data { get; }

    public ModVariableViewModel(ModViewModel mod, VarData data)
    {
        Mod = mod ?? throw new ArgumentNullException(nameof(mod));
        Data = data ?? throw new ArgumentNullException(nameof(data));

        IsSeparator = data.IsSeparator;
        Name = data.Name;
        CurrentValue = data.CurrentValue;
        OriginalValue = data.OriginalValue;
        SuggestedValue = data.SuggestedValue;
        PreviousValue = data.PreviousValue;
        Description = data.Description;
        NormalBrush = mod.ForegroundColor ?? Brushes.Gold;

        mod.PropertyChanged += Mod_PropertyChanged;
        PropertyChanged += OnPropertyChanged;
        UpdateForegrounds();
    }

    private void Mod_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "ForegroundColor")
            NormalBrush = Mod.ForegroundColor;
    }

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CurrentValue) && Data.CurrentValue != CurrentValue)
        {
            Data.CurrentValue = CurrentValue;
            Mod.Data.IsModified = Data.IsModified = true;
        }
    }

    [ObservableProperty]
    private bool _IsSeparator;

    [ObservableProperty]
    private string _Name;

    [ObservableProperty]
    private string _Description;

    [ObservableProperty]
    private string _CurrentValue;
    [ObservableProperty]
    private IBrush _CurrentValueForeground;

    [ObservableProperty]
    private string _OriginalValue;
    [ObservableProperty]
    private IBrush _OriginalValueForeground;

    [ObservableProperty]
    private string _SuggestedValue;
    [ObservableProperty]
    private IBrush _SuggestedValueForeground;

    [ObservableProperty]
    private string _PreviousValue;
    [ObservableProperty]
    private IBrush _PreviousValueForeground;


    [ObservableProperty]
    private IBrush _NormalBrush = new SolidColorBrush(Color.Parse("#00ffbf"));
    [ObservableProperty]
    private IBrush _CustomBrush = new SolidColorBrush(Color.Parse("#ffffff"));
    [ObservableProperty]
    private IBrush _DarkBrush = new SolidColorBrush(Color.Parse("#8f8f8f"));



    [RelayCommand]
    private void UseOriginal()
    {
        CurrentValue = OriginalValue;
        UpdateForegrounds();
    }

    [RelayCommand]
    private void UseSuggested()
    {
        CurrentValue = SuggestedValue;
        UpdateForegrounds();
    }

    [RelayCommand]
    private void UsePrevious()
    {
        CurrentValue = PreviousValue;
        UpdateForegrounds();
    }

    partial void OnCurrentValueChanged(string value)
    {
        UpdateForegrounds();
    }


    public void UpdateForegrounds()
    {
        CurrentValueForeground = GetCurrentValueForeground(CurrentValue);
        OriginalValueForeground = GetOriginalValueForeground();
        SuggestedValueForeground = GetSuggestedValueForeground();
        PreviousValueForeground = GetPreviousValueForeground();
    }

    public IBrush GetCurrentValueForeground(string currentValue)
    {
        bool equalsOriginal = string.Equals(currentValue, OriginalValue, StringComparison.Ordinal);
        bool equalsSuggested = string.Equals(currentValue, SuggestedValue, StringComparison.Ordinal);
        bool equalsPrevious = string.Equals(currentValue, PreviousValue, StringComparison.Ordinal);
        return
            equalsOriginal || equalsSuggested || equalsPrevious ? NormalBrush :
            CustomBrush;
    }

    public IBrush GetOriginalValueForeground()
    {
        bool equalsOriginal = string.Equals(CurrentValue, OriginalValue, StringComparison.Ordinal);
        return
            equalsOriginal ? NormalBrush :
            DarkBrush;
    }

    public IBrush GetSuggestedValueForeground()
    {
        bool equalsSuggested = string.Equals(CurrentValue, SuggestedValue, StringComparison.Ordinal);
        return
            equalsSuggested ? NormalBrush :
            DarkBrush;
    }

    public IBrush GetPreviousValueForeground()
    {
        bool equalsOriginal = string.Equals(CurrentValue, OriginalValue, StringComparison.Ordinal);
        bool equalsSuggested = string.Equals(CurrentValue, SuggestedValue, StringComparison.Ordinal);
        bool equalsPrevious = string.Equals(CurrentValue, PreviousValue, StringComparison.Ordinal);
        return
            equalsPrevious ? NormalBrush :
            DarkBrush;
    }

}
