using System.Windows;
using System.Windows.Controls;
using LenovoLaptopBacklight.ViewModels;

namespace LenovoLaptopBacklight.Views;

public partial class ControlView : UserControl
{
    public ControlView()
    {
        InitializeComponent();
        IsVisibleChanged += (_, e) =>
        {
            if ((bool)e.NewValue && DataContext is ControlViewModel vm)
                vm.Reload();
        };
    }
}
