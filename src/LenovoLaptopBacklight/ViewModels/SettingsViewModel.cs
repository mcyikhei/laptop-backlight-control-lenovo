using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using LenovoLaptopBacklight.Localization;
using LenovoLaptopBacklight.Models;
using LenovoLaptopBacklight.Services.Boot;
using LenovoLaptopBacklight.Services.Config;
using LenovoLaptopBacklight.Services.Defender;

namespace LenovoLaptopBacklight.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private static string L(string key) => Loc.I[key];

    private AppConfig _cfg = ConfigStore.Load();
    private bool _loading;

    private string _statusText = "";
    private int _register, _mirrorRegister;
    private string _newStageName = "";
    private bool _restartEnabled;
    private string? _restartStage;
    private string _defenderStatus = "";
    private string _selectedLanguage = "English";

    public string StatusText       { get => _statusText;       set => Set(ref _statusText, value); }
    public int    Register         { get => _register;         set => Set(ref _register, value); }
    public int    MirrorRegister   { get => _mirrorRegister;   set => Set(ref _mirrorRegister, value); }
    public string NewStageName     { get => _newStageName;     set => Set(ref _newStageName, value); }

    public string[] Languages { get; } = { "English", "中文" };
    public string SelectedLanguage { get => _selectedLanguage; set { if (Set(ref _selectedLanguage, value)) ApplyLanguage(); } }

    // "Apply on restart" setting
    public bool RestartEnabled { get => _restartEnabled; set { if (Set(ref _restartEnabled, value)) ApplyRestartSetting(); } }
    public string? RestartStage { get => _restartStage;  set { if (Set(ref _restartStage, value)) ApplyRestartSetting(); } }

    public string DefenderStatus { get => _defenderStatus; set => Set(ref _defenderStatus, value); }

    public ObservableCollection<Stage> Stages { get; } = new();
    public ObservableCollection<string> StageNames { get; } = new();

    public RelayCommand        SaveRegistersCommand { get; }
    public RelayCommand        AddStageCommand      { get; }
    public RelayCommand<Stage> DeleteStageCommand   { get; }
    public RelayCommand        ResetDefaultsCommand { get; }
    public RelayCommand        OpenLogCommand       { get; }
    public RelayCommand        OpenConfigCommand    { get; }
    public RelayCommand        AddExclusionCommand  { get; }
    public RelayCommand        RemoveExclusionCommand { get; }

    public SettingsViewModel()
    {
        SaveRegistersCommand = new RelayCommand(SaveRegisters);
        AddStageCommand      = new RelayCommand(AddStage, () => !string.IsNullOrWhiteSpace(NewStageName));
        DeleteStageCommand   = new RelayCommand<Stage>(DeleteStage, s => s != null && Stages.Count > 1);
        ResetDefaultsCommand = new RelayCommand(ResetDefaults);
        OpenLogCommand       = new RelayCommand(() => TryOpen(ConfigStore.LogPath));
        OpenConfigCommand    = new RelayCommand(() => TryOpen(ConfigStore.ConfigPath));
        AddExclusionCommand    = new RelayCommand(AddDefenderExclusion);
        RemoveExclusionCommand = new RelayCommand(RemoveDefenderExclusion);
        Reload();
    }

    private void ApplyLanguage()
    {
        if (_loading) return;
        var code = _selectedLanguage == "中文" ? "zh" : "en";
        Loc.I.SetLanguage(code);
        _cfg.Language = code;
        ConfigStore.Save(_cfg);
        RefreshDefenderStatus();
        StatusText = string.Format(L("set.configPathFmt"), ConfigStore.ConfigPath);
    }

    private void RefreshDefenderStatus()
        => DefenderStatus = DefenderManager.HasExclusion() ? L("set.defenderAllowed") : L("set.defenderNone");

    private void AddDefenderExclusion()
    {
        var (ok, msg) = DefenderManager.AddExclusion();
        StatusText = msg;
        RefreshDefenderStatus();
    }

    private void RemoveDefenderExclusion()
    {
        var (ok, msg) = DefenderManager.RemoveExclusion();
        StatusText = msg;
        RefreshDefenderStatus();
    }

    public void Reload()
    {
        _loading = true;
        _cfg = ConfigStore.Load();
        Register       = _cfg.Register;
        MirrorRegister = _cfg.MirrorRegister;
        _selectedLanguage = _cfg.Language == "zh" ? "中文" : "English";
        OnPropertyChanged(nameof(SelectedLanguage));

        Stages.Clear();
        foreach (var s in _cfg.Stages) Stages.Add(s);

        StageNames.Clear();
        foreach (var s in _cfg.Stages) StageNames.Add(s.Name);

        // restart setting
        _restartStage = StageNames.Contains(_cfg.BootStage)
            ? _cfg.BootStage
            : StageNames.FirstOrDefault();
        OnPropertyChanged(nameof(RestartStage));
        _restartEnabled = _cfg.BootEnabled && BootTaskManager.TaskExists();
        OnPropertyChanged(nameof(RestartEnabled));

        RefreshDefenderStatus();
        StatusText = string.Format(L("set.configPathFmt"), ConfigStore.ConfigPath);
        _loading = false;
    }

    private void ApplyRestartSetting()
    {
        if (_loading) return;

        _cfg.BootEnabled = RestartEnabled;
        if (!string.IsNullOrEmpty(RestartStage)) _cfg.BootStage = RestartStage;
        ConfigStore.Save(_cfg);

        try
        {
            if (RestartEnabled)
            {
                if (!_cfg.IsConfigured)
                {
                    StatusText = L("set.runDetectionFirst");
                    _loading = true; _restartEnabled = false; OnPropertyChanged(nameof(RestartEnabled)); _loading = false;
                    return;
                }
                // Ensure Defender lets the driver load at logon (clears the StartService 225 block)
                if (!DefenderManager.HasExclusion())
                {
                    DefenderManager.AddExclusion();
                    RefreshDefenderStatus();
                }
                BootTaskManager.Register(_cfg);
                StatusText = string.Format(L("set.onRestartSetFmt"), RestartStage);
            }
            else
            {
                BootTaskManager.Unregister();
                StatusText = L("set.restartOff");
            }
        }
        catch (Exception ex)
        {
            StatusText = string.Format(L("common.errorFmt"), ex.Message);
            MessageBox.Show(ex.Message, L("set.taskErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveRegisters()
    {
        _cfg.Register       = Register;
        _cfg.MirrorRegister = MirrorRegister;
        _cfg.Stages         = Stages.ToList();
        ConfigStore.Save(_cfg);
        RefreshStageNames();
        StatusText = L("set.saved");
    }

    private void RefreshStageNames()
    {
        var current = RestartStage;
        StageNames.Clear();
        foreach (var s in Stages) StageNames.Add(s.Name);
        _loading = true;
        _restartStage = StageNames.Contains(current ?? "") ? current : StageNames.FirstOrDefault();
        OnPropertyChanged(nameof(RestartStage));
        _loading = false;
    }

    private void AddStage()
    {
        if (string.IsNullOrWhiteSpace(NewStageName)) return;
        if (Stages.Any(s => s.Name.Equals(NewStageName, StringComparison.OrdinalIgnoreCase)))
        { StatusText = L("set.stageExists"); return; }
        Stages.Add(new Stage(NewStageName.Trim(), 0x00));
        NewStageName = "";
        SaveRegisters();
        StatusText = L("set.stageAdded");
    }

    private void DeleteStage(Stage? s)
    {
        if (s == null || Stages.Count <= 1) return;
        Stages.Remove(s);
        SaveRegisters();
        StatusText = string.Format(L("set.stageDeletedFmt"), s.Name);
    }

    private void ResetDefaults()
    {
        if (MessageBox.Show(L("set.resetStagesConfirm"), L("set.resetStagesTitle"),
            MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        _cfg.Stages = new AppConfig().Stages;
        Stages.Clear();
        foreach (var s in _cfg.Stages) Stages.Add(s);
        ConfigStore.Save(_cfg);
        RefreshStageNames();
        StatusText = L("set.stagesReset");
    }

    private static void TryOpen(string path)
    {
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
        catch (Exception ex) { MessageBox.Show(ex.Message, L("common.errorTitle"), MessageBoxButton.OK, MessageBoxImage.Error); }
    }
}
