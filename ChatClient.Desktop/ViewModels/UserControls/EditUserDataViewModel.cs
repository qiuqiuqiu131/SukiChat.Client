using System;
using System.Collections.Generic;
using ChatClient.Desktop.UIEntity;
using ChatClient.Desktop.Views.UserControls;
using ChatClient.Tool.Data;
using ChatClient.Tool.Events;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Mvvm;

namespace ChatClient.Desktop.ViewModels.UserControls;

public class EditUserDataViewModel : BindableBase, IDialogAware
{
    private readonly IEventAggregator _eventAggregator;
    private readonly IDialogService _dialogService;

    private UserDto _userDto;

    public UserDto UserDto
    {
        get => _userDto;
        set => SetProperty(ref _userDto, value);
    }

    private string name;

    public string Name
    {
        get => name;
        set => SetProperty(ref name, value);
    }

    private string introduction;

    public string Introduction
    {
        get => introduction;
        set => SetProperty(ref introduction, value);
    }

    private Sex sex;

    public Sex Sex
    {
        get => sex;
        set => SetProperty(ref sex, value);
    }

    public IEnumerable<Sex> SexEnum { get; set; } = Enum.GetValues<Sex>();

    public DelegateCommand EditHeadCommand { get; init; }
    public DelegateCommand CancelCommand { get; init; }

    public EditUserDataViewModel(IEventAggregator eventAggregator,
        IDialogService dialogService)
    {
        _eventAggregator = eventAggregator;
        _dialogService = dialogService;

        EditHeadCommand = new DelegateCommand(EditHead);
        CancelCommand = new DelegateCommand(() => RequestClose.Invoke());
    }

    private void EditHead()
    {
        _dialogService.Show(nameof(UserHeadEditView));
    }

    #region Dialog

    public bool CanCloseDialog() => true;

    public void OnDialogClosed()
    {
        _eventAggregator.GetEvent<DialogShowEvent>().Publish(false);
    }

    public void OnDialogOpened(IDialogParameters parameters)
    {
        UserDto = parameters.GetValue<UserDto>("UserDto");
        Name = UserDto.Name;
        Introduction = UserDto.Introduction ?? "";
        Sex = UserDto.Sex;

        _eventAggregator.GetEvent<DialogShowEvent>().Publish(true);
    }

    public DialogCloseListener RequestClose { get; }

    #endregion
}