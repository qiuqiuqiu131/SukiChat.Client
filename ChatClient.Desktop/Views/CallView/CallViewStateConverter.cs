using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using ChatClient.Desktop.ViewModels.CallViewModel;

namespace ChatClient.Desktop.Views.CallView;

public class CallViewStateConverter : MarkupExtension, IValueConverter
{
    public bool IsEquals { get; set; } = true;
    public CallViewState State { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return this;
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is CallViewState callViewState)
        {
            return State == callViewState ? IsEquals : !IsEquals;
        }

        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}