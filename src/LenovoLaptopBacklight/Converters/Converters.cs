using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LenovoLaptopBacklight.Converters;

[ValueConversion(typeof(bool), typeof(Visibility))]
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is true ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => value is Visibility.Collapsed;
}

[ValueConversion(typeof(bool), typeof(bool))]
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
        => value is bool b && !b;
    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => value is bool b && !b;
}

/// <summary>
/// Converts an integer to Visibility.Visible when it equals ConverterParameter.
/// Used for wizard step panels: Visibility="{Binding Step, Converter={StaticResource EqToVis}, ConverterParameter=2}"
/// </summary>
[ValueConversion(typeof(int), typeof(Visibility))]
public class EqualityToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object p, CultureInfo c)
    {
        if (value == null || p == null) return Visibility.Collapsed;
        return value.ToString() == p.ToString() ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object value, Type t, object p, CultureInfo c)
        => Binding.DoNothing;
}
