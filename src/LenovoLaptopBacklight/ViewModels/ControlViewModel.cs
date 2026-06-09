using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using LenovoLaptopBacklight.Localization;
using LenovoLaptopBacklight.Models;
using LenovoLaptopBacklight.Services.Config;
using LenovoLaptopBacklight.Services.Ec;

namespace LenovoLaptopBacklight.ViewModels;

public class ControlViewModel : ViewModelBase
{
    private static string L(string key) => Loc.I[key];

    private AppConfig _cfg = ConfigStore.Load();

    private string _statusText = "";
    private Stage? _currentStage;
    private string _currentRawValue = "–";
    private bool _isConfigured;

    public ObservableCollection<Stage> Stages { get; } = new();

    public string StatusText    { get => _statusText;    set => Set(ref _statusText, value); }
    public Stage? CurrentStage  { get => _currentStage;  set => Set(ref _currentStage, value); }
    public string CurrentRawValue { get => _currentRawValue; set => Set(ref _currentRawValue, value); }
    public bool IsConfigured    { get => _isConfigured;  set => Set(ref _isConfigured, value); }

    public RelayCommand<Stage> ApplyStageCommand { get; }
    public RelayCommand        ReadCurrentCommand { get; }

    public ControlViewModel()
    {
        ApplyStageCommand = new RelayCommand<Stage>(ApplyStage, s => IsConfigured && s != null);
        ReadCurrentCommand = new RelayCommand(ReadCurrent, () => IsConfigured);
        Reload();
    }

    public void Reload()
    {
        _cfg = ConfigStore.Load();
        Stages.Clear();
        foreach (var s in _cfg.Stages) Stages.Add(s);
        IsConfigured = _cfg.IsConfigured;
        StatusText = IsConfigured
            ? string.Format(L("control.status.registerFmt"), _cfg.Register)
            : L("control.status.notConfigured");
    }

    private async void ApplyStage(Stage? stage)
    {
        if (stage == null || !_cfg.IsConfigured) return;
        int reg = _cfg.Register;
        byte val = stage.Value;
        try
        {
            // Interactive writes use the PRIMARY register only — identical to the Detection
            // Wizard's Verify step, which is the reliable path. Writing the mirror register
            // back-to-back here was preventing the LED from updating on some ECs.
            // (The boot-time --apply still writes the mirror for persistence.)
            var ec = EcAccess.Instance;
            byte rb = await Task.Run(() =>
            {
                ec.WriteByte(reg, val);
                return ec.ReadByte(reg);
            });
            CurrentRawValue = $"0x{rb:X2}";
            CurrentStage = _cfg.Stages.FirstOrDefault(s => s.Value == rb);
            ConfigStore.Log($"UI apply '{stage.Name}': wrote 0x{val:X2} to reg 0x{reg:X2}, readback 0x{rb:X2}");
            StatusText = rb == val
                ? string.Format(L("control.status.appliedFmt"), stage.Name, val)
                : string.Format(L("control.status.mismatchFmt"), stage.Name, val, rb);
        }
        catch (Exception ex)
        {
            StatusText = string.Format(L("common.errorFmt"), ex.Message);
            MessageBox.Show(ex.Message, L("control.error.title"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ReadCurrent()
    {
        if (!_cfg.IsConfigured) return;
        int reg = _cfg.Register;
        try
        {
            byte val = await Task.Run(() => EcAccess.Instance.ReadByte(reg));
            CurrentRawValue = $"0x{val:X2}";
            CurrentStage = _cfg.Stages.FirstOrDefault(s => s.Value == val);
        }
        catch (Exception ex) { CurrentRawValue = $"Error: {ex.Message}"; }
    }
}
