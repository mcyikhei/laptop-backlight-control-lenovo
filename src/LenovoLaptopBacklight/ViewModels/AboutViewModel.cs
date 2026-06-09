using System.Diagnostics;
using System.Reflection;
using System.Windows;
using LenovoLaptopBacklight.Localization;

namespace LenovoLaptopBacklight.ViewModels;

public class AboutViewModel : ViewModelBase
{
    private const string Repo = "https://github.com/mcyikhei/laptop-backlight-control-lenovo";

    public AboutViewModel()
    {
        // Refresh all localized text when the language changes.
        Loc.I.PropertyChanged += (_, _) => OnPropertyChanged(null);
    }

    public string AppName    => Loc.I["about.appName"];
    public string Version    => string.Format(Loc.I["about.versionFmt"],
                                    Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0");
    public string AuthorLine => Loc.I["about.author"] + ": mcyikhei";
    public string GitHubLabel => Loc.I["about.github"];
    public string AuthorHeader => Loc.I["about.author"];
    public string CopyLabel  => Loc.I["about.copy"];
    public string DisclaimerTitle => Loc.I["about.disclaimerTitle"];
    public string CreditsTitle => Loc.I["about.creditsTitle"];
    public string License    => Loc.I["about.license"];
    public string Disclaimer => Loc.I["about.disclaimer"];
    public string Credits    => Loc.I["about.credits"];
    public string RepoUrl    => Repo;

    public RelayCommand OpenRepoCommand { get; } =
        new(() => Process.Start(new ProcessStartInfo(Repo) { UseShellExecute = true }));

    public RelayCommand CopyRepoCommand { get; } =
        new(() => Clipboard.SetText(Repo));
}
