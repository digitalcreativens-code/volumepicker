using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VolumeGuard.ViewModels;

public sealed class StatusViewModel : INotifyPropertyChanged
{
    private bool _protectionEnabled = true;
    private bool _watchdogAlive;
    private bool _startupEnabled;
    private double _allowedPercent;
    private double _currentPercent;
    private string _slotDescription = "";

    public bool ProtectionEnabled
    {
        get => _protectionEnabled;
        set => SetField(ref _protectionEnabled, value);
    }

    public bool WatchdogAlive
    {
        get => _watchdogAlive;
        set => SetField(ref _watchdogAlive, value);
    }

    public bool StartupEnabled
    {
        get => _startupEnabled;
        set => SetField(ref _startupEnabled, value);
    }

    public double AllowedPercent
    {
        get => _allowedPercent;
        set => SetField(ref _allowedPercent, value);
    }

    public double CurrentPercent
    {
        get => _currentPercent;
        set => SetField(ref _currentPercent, value);
    }

    public string SlotDescription
    {
        get => _slotDescription;
        set => SetField(ref _slotDescription, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
}
