using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ChatClient.Avalonia.Controls.FormControls;
using ChatClient.Desktop.ViewModels.SukiDialogs;

namespace ChatClient.Desktop.Views.SukiDialog;

public partial class EditUserDataView : UserControl
{
    public EditUserDataView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        var dateTime = ((EditUserDataViewModel)DataContext!).DateTime;
        DateTime date;

        if (dateTime == null)
            date = DateTime.Now;
        else
            date = new DateTime(dateTime.Value.Year, dateTime.Value.Month, dateTime.Value.Day);

        datePicker.SelectedDate = new DateTimeOffset(date);
        datePicker.MaxYear = new DateTimeOffset(DateTime.Now);
        datePicker.MinYear = new DateTimeOffset(DateTime.Now - TimeSpan.FromDays(365 * 100));
        datePicker.SelectedDateChanged += DatePickerOnSelectedDateChanged;
    }

    private void DatePickerOnSelectedDateChanged(object? sender, DatePickerSelectedValueChangedEventArgs e)
    {
        if (sender is FormDatePicker datePicker)
        {
            ((EditUserDataViewModel)DataContext!).DateTime = new DateOnly(datePicker.SelectedDate.Value.DateTime.Year,
                datePicker.SelectedDate.Value.DateTime.Month, datePicker.SelectedDate.Value.DateTime.Day);
        }
    }
}