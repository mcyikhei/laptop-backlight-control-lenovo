using System;
using System.Windows.Data;
using System.Windows.Markup;

namespace LenovoLaptopBacklight.Localization;

/// <summary>
/// XAML markup extension: {loc:Tr key} — binds the target to Loc.I[key] (OneWay).
/// Because Loc raises the indexer PropertyChanged on SetLanguage, every usage updates live.
/// </summary>
[MarkupExtensionReturnType(typeof(object))]
public class TrExtension : MarkupExtension
{
    public string Key { get; set; } = "";

    public TrExtension() { }
    public TrExtension(string key) { Key = key; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding($"[{Key}]")
        {
            Source = Loc.I,
            Mode = BindingMode.OneWay
        };
        return binding.ProvideValue(serviceProvider);
    }
}
