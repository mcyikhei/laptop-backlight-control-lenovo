using System;
using System.Collections.Generic;
using System.Linq;
using LenovoLaptopBacklight.Services.Ec;

namespace LenovoLaptopBacklight.Services.Detection;

public record RegisterCandidate(int Register, Dictionary<string, byte> StateValues, int DistinctCount);

/// <summary>
/// Port of capture-states.ps1:
/// Captures EC dumps per named stage, then finds registers that are
/// constant within each stage but differ across stages — the backlight register.
/// </summary>
public class RegisterDetector
{
    private readonly EcController _ec;

    // stage name -> list of 256-byte dumps
    private readonly Dictionary<string, List<byte[]>> _captures = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<string> CapturedStages => _captures.Keys.ToList();

    public RegisterDetector(EcController ec) { _ec = ec; }

    /// <summary>Capture N EC dumps for a stage. Call while backlight is in that state.</summary>
    public void CaptureStage(string stageName, int samples = 3)
    {
        var dumps = new List<byte[]>();
        for (int i = 0; i < samples; i++)
        {
            var dump = _ec.DumpAll();
            dumps.Add(dump);
            System.Threading.Thread.Sleep(300);
        }
        _captures[stageName] = dumps;
    }

    /// <summary>
    /// Analyse captured dumps and return registers ranked by how cleanly they track the stages.
    /// Best candidates: DistinctCount == number of stages (one unique value per stage).
    /// </summary>
    public List<RegisterCandidate> FindCandidates()
    {
        if (_captures.Count < 2)
            throw new InvalidOperationException("Need at least 2 stages captured.");

        var stageNames = _captures.Keys.ToList();
        var candidates = new List<RegisterCandidate>();

        for (int reg = 0; reg < 256; reg++)
        {
            var perState = new Dictionary<string, byte>();
            bool constantInAllStates = true;

            foreach (var stage in stageNames)
            {
                var vals = _captures[stage].Select(d => d[reg]).Distinct().ToList();
                if (vals.Count != 1) { constantInAllStates = false; break; }
                perState[stage] = vals[0];
            }

            if (!constantInAllStates) continue;

            int distinct = perState.Values.Distinct().Count();
            if (distinct >= 2)
                candidates.Add(new RegisterCandidate(reg, perState, distinct));
        }

        // Best candidates first: most distinct values, then lowest register number
        return candidates
            .OrderByDescending(c => c.DistinctCount)
            .ThenBy(c => c.Register)
            .ToList();
    }

    public void Clear() => _captures.Clear();
}
