using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using ChatClient.BaseService.Services.Interface;
using ChatClient.Desktop.Views.UserControls;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
using ChatClient.Tool.Tools;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Ioc;
using Prism.Mvvm;

namespace ChatClient.Desktop.ViewModels.UserControls;

public class UserHeadEditViewModel : BindableBase, IDialogAware
{
    #region Properties

    private Bitmap _currentHead;

    public Bitmap CurrentHead
    {
        get => _currentHead;
        set => SetProperty(ref _currentHead, value);
    }

    private Bitmap _selectedItem;

    public Bitmap SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    private bool isBusy = false;

    public bool IsBusy
    {
        get => isBusy;
        set => SetProperty(ref isBusy, value);
    }

    private ObservableCollection<Bitmap> _headList;

    public ObservableCollection<Bitmap> HeadList
    {
        get => _headList;
        set => SetProperty(ref _headList, value);
    }

    public DelegateCommand AddHeadCommnad { get; init; }
    public DelegateCommand SaveHeadCommand { get; init; }
    public DelegateCommand SelectedHeadChangedCommand { get; init; }
    public DelegateCommand CancelCommand { get; init; }

    #endregion

    private Dictionary<int, Bitmap> _headImages;

    public event Action<Bitmap> ImageChanged;
    public Control? View;

    private bool isNewHead = false;

    private readonly IUserManager _userManager;
    private readonly IContainerProvider _containerProvider;

    public UserHeadEditViewModel(IUserManager userManager, IContainerProvider containerProvider)
    {
        _userManager = userManager;
        _containerProvider = containerProvider;
        CurrentHead = userManager.User!.UserDto.HeadImage;

        AddHeadCommnad = new DelegateCommand(AddHead);
        SaveHeadCommand = new DelegateCommand(SaveHead);
        SelectedHeadChangedCommand = new DelegateCommand(SelectedHeadChanged);
        CancelCommand = new DelegateCommand(() => RequestClose.Invoke());

        if (_userManager.User?.UserDto.HeadCount != 0)
        {
            var userService = _containerProvider.Resolve<IUserService>();
            userService.GetHeadImages(userManager.User.UserDto).ContinueWith(list =>
            {
                _headImages = list.Result;
                var bitmaps = list.Result.Values.ToList();
                bitmaps.Reverse();
                HeadList = new ObservableCollection<Bitmap>(bitmaps);

                if (_headImages.TryGetValue(userManager.User.UserDto.HeadIndex, out var image))
                    SelectedItem = image;
            });
        }
        else
        {
            HeadList = new ObservableCollection<Bitmap>();
            _headImages = new Dictionary<int, Bitmap>();
        }
    }

    private void SelectedHeadChanged()
    {
        ImageChanged?.Invoke(SelectedItem);
        isNewHead = false;
    }

    private async void AddHead()
    {
        if (View == null) return;

        string filePath = "";
        var window = TopLevel.GetTopLevel(View);
        var handle = window.TryGetPlatformHandle()?.Handle;

        if (handle == null) return;

        var systemFileDialog = _containerProvider.Resolve<ISystemFileDialog>();
        filePath = await systemFileDialog.OpenFileAsync(handle.Value, "选择图片", "",
            ["Image Files", "*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.tiff;*.webp"]);

        if (string.IsNullOrWhiteSpace(filePath)) return;

        // 选择好图像后，直接显示在页面上，但是暂时不许要上传。等用户设置好头像位置，点击确认后，在保存图片并上传。
        Bitmap? bitmap = null;
        try
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                bitmap = new Bitmap(stream);
        }
        catch (Exception e)
        {
            return;
        }

        if (bitmap == null) return;

        // 读取选中图片，显示出来
        ImageChanged?.Invoke(bitmap);
        isNewHead = true;
    }

    private async void SaveHead()
    {
        IsBusy = true;
        UserHeadEditView view = View as UserHeadEditView;
        if (view == null) return;

        var imageResize = view.GetImageResize();
        if (!isNewHead && imageResize.Scale == 1 && imageResize.MoveX == 0 && imageResize.MoveY == 0)
        {
            IsBusy = false;
            _userManager.User.UserDto.HeadImage = SelectedItem;
            _userManager.User.UserDto.HeadIndex = _headImages.FirstOrDefault(x => x.Value == SelectedItem).Key;
            await _userManager.SaveUser();

            foreach (var value in _headImages.Values)
                if (value != SelectedItem)
                    value.Dispose();
            _headImages.Clear();

            RequestClose.Invoke();

            return;
        }

        // 分割图片
        Bitmap bitmap = ImageTool.GetHeadImage(imageResize);

        var result = await _userManager.ResetHead(bitmap);
        IsBusy = false;
        if (result)
        {
            HeadList.Insert(0, bitmap);
            _headImages.Add(_userManager.User.UserDto.HeadIndex, bitmap);
            SelectedItem = bitmap;
            ImageChanged?.Invoke(bitmap);

            foreach (var value in _headImages.Values)
                value.Dispose();
            _headImages.Clear();

            RequestClose.Invoke();
        }
        else
        {
            // 通知
        }
    }

    #region DialogAware

    public bool CanCloseDialog() => true;

    public void OnDialogClosed()
    {
    }

    public void OnDialogOpened(IDialogParameters parameters)
    {
    }

    public DialogCloseListener RequestClose { get; }

    #endregion
}