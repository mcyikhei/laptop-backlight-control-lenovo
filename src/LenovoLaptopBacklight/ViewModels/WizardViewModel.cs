using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using LenovoLaptopBacklight.Localization;
using LenovoLaptopBacklight.Models;
using LenovoLaptopBacklight.Services.Config;
using LenovoLaptopBacklight.Services.Detection;
using LenovoLaptopBacklight.Services.Ec;

namespace LenovoLaptopBacklight.ViewModels;

public class WizardStageItem : ViewModelBase
{
    private bool _captured;
    private bool _skipped;
    public string Name { get; set; } = "";
    public bool Captured { get => _captured; set { if (Set(ref _captured, value)) OnPropertyChanged(nameof(StatusGlyph)); } }
    public bool Skipped  { get => _skipped;  set { if (Set(ref _skipped, value))  OnPropertyChanged(nameof(StatusGlyph)); } }

    public string StatusGlyph => Captured ? "✔" : Skipped ? "⤬" : "○";
    public bool IsPending => !Captured && !Skipped;
}

public class WizardViewModel : ViewModelBase
{
    private static string L(string key) => Loc.I[key];

    private const int VerifyDwellMs = 3500; // how long each stage is shown during Verify

    private RegisterDetector? _detector;
    private AppConfig _cfg = ConfigStore.Load();

    private int _step = 0;          // 0=intro, 1=capture, 2=results, 3=verify-running, 4=confirm, 5=done
    private string _statusText = Loc.I["wiz.welcome"];
    private string _instructionText = "";
    private bool _isBusy;
    private WizardStageItem? _currentCaptureStage;
    private string _candidatesSummary = "";
    private int _selectedRegister = -1;
    private string _verifyStageName = "";
    private string _verifyStageDetail = "";

    // Setters that affect command availability re-query commands immediately, so buttons
    // don't stay greyed/enabled until the next UI event.
    public int Step                     { get => _step;                set { if (Set(ref _step, value)) Requery(); } }
    public string StatusText            { get => _statusText;          set => Set(ref _statusText, value); }
    public string InstructionText       { get => _instructionText;     set => Set(ref _instructionText, value); }
    public bool IsBusy                  { get => _isBusy;              set { if (Set(ref _isBusy, value)) Requery(); } }
    public WizardStageItem? CurrentCaptureStage { get => _currentCaptureStage; set { if (Set(ref _currentCaptureStage, value)) Requery(); } }
    public string CandidatesSummary     { get => _candidatesSummary;   set => Set(ref _candidatesSummary, value); }
    public int SelectedRegister         { get => _selectedRegister;    set { if (Set(ref _selectedRegister, value)) Requery(); } }

    private static void Requery() => CommandManager.InvalidateRequerySuggested();
    public string VerifyStageName       { get => _verifyStageName;     set => Set(ref _verifyStageName, value); }
    public string VerifyStageDetail     { get => _verifyStageDetail;   set => Set(ref _verifyStageDetail, value); }

    public ObservableCollection<WizardStageItem> CaptureStages { get; } = new();
    public ObservableCollection<string> CandidateList { get; } = new();

    public RelayCommand StartCommand       { get; }
    public RelayCommand CaptureNextCommand { get; }
    public RelayCommand SkipCommand        { get; }
    public RelayCommand AnalyseCommand     { get; }
    public RelayCommand VerifyCommand      { get; }
    public RelayCommand SaveCommand        { get; }
    public RelayCommand ResetCommand       { get; }

    private int CapturedCount => CaptureStages.Count(s => s.Captured);
    private bool AllResolved  => CaptureStages.All(s => !s.IsPending);

    public WizardViewModel()
    {
        StartCommand       = new RelayCommand(Start,       () => Step == 0);
        CaptureNextCommand = new RelayCommand(CaptureNext, () => Step == 1 && !IsBusy && CurrentCaptureStage is { IsPending: true });
        SkipCommand        = new RelayCommand(Skip,        () => Step == 1 && !IsBusy && CurrentCaptureStage is { IsPending: true });
        AnalyseCommand     = new RelayCommand(Analyse,     () => Step == 1 && !IsBusy && AllResolved && CapturedCount >= 2);
        VerifyCommand      = new RelayCommand(Verify,      () => Step == 2 && SelectedRegister >= 0 && !IsBusy);
        SaveCommand        = new RelayCommand(Save,        () => Step == 4 && SelectedRegister >= 0);
        ResetCommand       = new RelayCommand(Reset);
        BuildStageList();
    }

    private void BuildStageList()
    {
        CaptureStages.Clear();
        foreach (var s in _cfg.Stages)
            CaptureStages.Add(new WizardStageItem { Name = s.Name });
        CurrentCaptureStage = CaptureStages.FirstOrDefault();
    }

    private void Start()
    {
        try
        {
            _detector = new RegisterDetector(EcAccess.Instance);
        }
        catch (Exception ex)
        {
            MessageBox.Show(string.Format(L("wiz.driverErrorFmt"), ex.Message),
                L("wiz.driverErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        Step = 1;
        UpdateCaptureInstruction();
    }

    private async void CaptureNext()
    {
        if (CurrentCaptureStage == null) return;
        IsBusy = true;
        var item = CurrentCaptureStage;
        StatusText = string.Format(L("wiz.capturingFmt"), item.Name);
        try
        {
            await Task.Run(() => _detector!.CaptureStage(item.Name));
            item.Captured = true;
            StatusText = string.Format(L("wiz.capturedFmt"), item.Name);
            AdvanceToNextPending();
        }
        catch (Exception ex) { StatusText = string.Format(L("common.errorFmt"), ex.Message); }
        finally { IsBusy = false; }
    }

    private void Skip()
    {
        if (CurrentCaptureStage == null) return;
        CurrentCaptureStage.Skipped = true;
        StatusText = string.Format(L("wiz.skippedFmt"), CurrentCaptureStage.Name);
        AdvanceToNextPending();
    }

    private void AdvanceToNextPending()
    {
        CurrentCaptureStage = CaptureStages.FirstOrDefault(s => s.IsPending);
        UpdateCaptureInstruction();
    }

    private void UpdateCaptureInstruction()
    {
        if (CurrentCaptureStage != null)
            InstructionText = string.Format(L("wiz.instrSetFmt"), CurrentCaptureStage.Name);
        else if (CapturedCount >= 2)
            InstructionText = L("wiz.instrAllResolved");
        else
            InstructionText = L("wiz.instrNeedTwo");
    }

    private async void Analyse()
    {
        IsBusy = true;
        StatusText = L("wiz.analysing");
        try
        {
            var candidates = await Task.Run(() => _detector!.FindCandidates());
            CandidateList.Clear();
            foreach (var c in candidates.Take(8))
            {
                var vals = string.Join(", ", c.StateValues.Select(kv => $"{kv.Key}=0x{kv.Value:X2}"));
                CandidateList.Add(string.Format(L("wiz.candidateRowFmt"), c.Register, vals, c.DistinctCount));
            }
            if (candidates.Count > 0)
            {
                SelectedRegister = candidates[0].Register;
                CandidatesSummary = string.Format(L("wiz.bestCandidateFmt"), candidates[0].Register, candidates[0].DistinctCount, CapturedCount);
            }
            else
            {
                CandidatesSummary = L("wiz.noCandidate");
            }
            Step = 2;
        }
        catch (Exception ex) { StatusText = string.Format(L("common.errorFmt"), ex.Message); }
        finally { IsBusy = false; }
    }

    private async void Verify()
    {
        var candidates = _detector!.FindCandidates();
        var best = candidates.FirstOrDefault(c => c.Register == SelectedRegister) ?? candidates.FirstOrDefault();
        if (best == null) { StatusText = L("wiz.noCandidatesVerify"); return; }

        IsBusy = true;
        Step = 3; // verify-running screen
        StatusText = L("wiz.verifyWatch");
        try
        {
            var ec = EcAccess.Instance;
            int n = best.StateValues.Count;
            int idx = 0;
            foreach (var kv in best.StateValues)
            {
                idx++;
                VerifyStageName = kv.Key;
                VerifyStageDetail = string.Format(L("wiz.verifyStepFmt"), idx, n, kv.Value, best.Register);
                await Task.Run(() => ec.WriteByte(best.Register, kv.Value)); // primary only — reliable
                await Task.Delay(VerifyDwellMs);
            }
            VerifyStageName = L("wiz.verifyDone");
            VerifyStageDetail = L("wiz.verifyDoneDetail");
            StatusText = L("wiz.verifyFinished");
            Step = 4; // confirm screen (Save or Reset)
        }
        catch (Exception ex)
        {
            StatusText = string.Format(L("wiz.verifyErrorFmt"), ex.Message);
            Step = 2;
        }
        finally { IsBusy = false; }
    }

    private void Save()
    {
        var candidates = _detector!.FindCandidates();
        var best = candidates.FirstOrDefault(c => c.Register == SelectedRegister);
        if (best == null) return;

        _cfg = ConfigStore.Load();
        _cfg.Register = best.Register;

        // Detect a mirror register (same value pattern at register+1, e.g. 0xAF mirrors 0xAE)
        var mirror = candidates.FirstOrDefault(c =>
            c.Register == best.Register + 1 &&
            c.StateValues.OrderBy(x => x.Key).SequenceEqual(best.StateValues.OrderBy(x => x.Key)));
        _cfg.MirrorRegister = mirror?.Register ?? -1;

        // Rebuild stages from the captured states only (skipped stages are dropped)
        _cfg.Stages = best.StateValues.Select(kv => new Stage(kv.Key, kv.Value)).ToList();

        // Make sure the boot stage still refers to an existing stage
        if (!_cfg.Stages.Any(s => s.Name.Equals(_cfg.BootStage, StringComparison.OrdinalIgnoreCase)))
        {
            _cfg.BootStage = _cfg.Stages.FirstOrDefault(s => s.Name.Equals("Off", StringComparison.OrdinalIgnoreCase))?.Name
                             ?? _cfg.Stages.FirstOrDefault()?.Name ?? "Off";
        }

        ConfigStore.Save(_cfg);
        ConfigStore.Log($"Wizard: saved register 0x{_cfg.Register:X2}, mirror={_cfg.MirrorRegister}, stages={_cfg.Stages.Count}");
        StatusText = L("wiz.savedOk");
        Step = 5;
    }

    private void Reset()
    {
        _detector?.Clear();
        _detector = null;
        Step = 0;
        _cfg = ConfigStore.Load();
        BuildStageList();
        StatusText = L("wiz.welcome");
        InstructionText = "";
        CandidateList.Clear();
        CandidatesSummary = "";
        SelectedRegister = -1;
        VerifyStageName = "";
        VerifyStageDetail = "";
    }
}
