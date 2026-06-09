using System.Windows;
using System.Windows.Controls;
using LenovoLaptopBacklight.ViewModels;

namespace LenovoLaptopBacklight.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        IsVisibleChanged += (_, e) =>
        {
            if ((bool)e.NewValue && DataContext is SettingsViewModel vm)
                vm.Reload();
        };
    }
}
