using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using ChatClient.BaseService.Manager;
using ChatClient.Desktop.Tool;
using ChatClient.Media;
using ChatClient.Media.Audio;
using ChatClient.Media.CallManager;
using ChatClient.Media.CallOperator;
using ChatClient.Tool.Data;
using ChatClient.Tool.Events;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
using ChatServer.Common.Protobuf;
using Prism.Commands;
using Prism.Dialogs;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SIPSorcery.Net;

namespace ChatClient.Desktop.ViewModels.CallViewModel;

public class VideoCallViewModel : BindableBase, IDialogAware, ICallView
{
    private readonly IUserDtoManager _userDtoManager;
    private readonly IUserManager _userManager;
    private readonly IMessageHelper _messageHelper;
    private readonly IEventAggregator _eventAggregator;
    private readonly IContainerProvider _containerProvider;

    private List<SubscriptionToken> _subscriptions = [];

    private AudioPlayer? _audioPlayer;

    private bool isAudioOver;

    private VideoCallOperator? _videoCallOperator;

    private CancellationTokenSource? _cancellationTokenSource;

    private FriendRelationDto? userTarget;

    public FriendRelationDto? UserTarget
    {
        get => userTarget;
        set
        {
            if (SetProperty(ref userTarget, value))
                RaisePropertyChanged(nameof(NickName));
        }
    }

    private bool isRemoteMainVideo = true;

    public bool IsRemoteMainVideo
    {
        get => isRemoteMainVideo;
        set
        {
            if (SetProperty(ref isRemoteMainVideo, value))
            {
                RaisePropertyChanged(nameof(MainVideoFrame));
                RaisePropertyChanged(nameof(SubVideoFrame));
                RaisePropertyChanged(nameof(IsMainVideoOpened));
                RaisePropertyChanged(nameof(IsSubVideoOpened));
            }
        }
    }

    // 在 ViewModel 中添加属性
    private WriteableBitmap? _localVideoFrame;

    private WriteableBitmap? LocalVideoFrame
    {
        get => _localVideoFrame;
        set
        {
            SetProperty(ref _localVideoFrame, value);
            RaisePropertyChanged(nameof(MainVideoFrame));
            RaisePropertyChanged(nameof(SubVideoFrame));
        }
    }

    private WriteableBitmap? _remoteVideoFrame;

    private WriteableBitmap? RemoteVideoFrame
    {
        get => _remoteVideoFrame;
        set
        {
            SetProperty(ref _remoteVideoFrame, value);
            RaisePropertyChanged(nameof(MainVideoFrame));
            RaisePropertyChanged(nameof(SubVideoFrame));
        }
    }

    // 添加主副视频源属性
    public WriteableBitmap? MainVideoFrame => IsRemoteMainVideo ? RemoteVideoFrame : LocalVideoFrame;
    public WriteableBitmap? SubVideoFrame => IsRemoteMainVideo ? LocalVideoFrame : RemoteVideoFrame;

    private bool isRemoteAudioOpened = true;

    public bool IsRemoteAudioOpened
    {
        get => isRemoteAudioOpened;
        set => SetProperty(ref isRemoteAudioOpened, value);
    }

    private bool isRemoteVideoOpened = false;

    private bool IsRemoteVideoOpened
    {
        get => isRemoteVideoOpened;
        set
        {
            if (SetProperty(ref isRemoteVideoOpened, value))
            {
                RaisePropertyChanged(nameof(IsMainVideoOpened));
                RaisePropertyChanged(nameof(IsSubVideoOpened));
            }
        }
    }

    private bool isLocalVideoOpened = false;

    private bool IsLocalVideoOpened
    {
        get => isLocalVideoOpened;
        set
        {
            if (SetProperty(ref isLocalVideoOpened, value))
            {
                RaisePropertyChanged(nameof(IsMainVideoOpened));
                RaisePropertyChanged(nameof(IsSubVideoOpened));
            }
        }
    }

    public bool IsMainVideoOpened => IsRemoteMainVideo ? IsRemoteVideoOpened : IsLocalVideoOpened;
    public bool IsSubVideoOpened => IsRemoteMainVideo ? IsLocalVideoOpened : IsRemoteVideoOpened;


    public string peerId => UserTarget != null ? UserTarget.Id : "";

    public string NickName => UserTarget != null ? UserTarget.Remark ?? UserTarget.UserDto!.Name : "";

    private string message;

    public string Message
    {
        get => message;
        set => SetProperty(ref message, value);
    }

    // 是否为发送者
    private bool isSender;

    public bool IsSender
    {
        get => isSender;
        set => SetProperty(ref isSender, value);
    }

    private CallViewState state;

    public CallViewState State
    {
        get => state;
        set => SetProperty(ref state, value);
    }

    public AsyncDelegateCommand HangUpCommand { get; set; }
    public AsyncDelegateCommand AcceptCommand { get; set; }
    public AsyncDelegateCommand<bool> SwitchAudioCommand { get; set; }
    public AsyncDelegateCommand<bool> SwitchVideoCommand { get; set; }
    public DelegateCommand SwitchMainVideoCommand { get; set; }

    public VideoCallViewModel(IUserDtoManager userDtoManager, IUserManager userManager,
        IMessageHelper messageHelper,
        IEventAggregator eventAggregator,
        IContainerProvider containerProvider)
    {
        _userDtoManager = userDtoManager;
        _userManager = userManager;
        _messageHelper = messageHelper;
        _eventAggregator = eventAggregator;
        _containerProvider = containerProvider;

        HangUpCommand = new AsyncDelegateCommand(OnHangUp);
        AcceptCommand = new AsyncDelegateCommand(OnAccept);
        SwitchAudioCommand = new AsyncDelegateCommand<bool>(OnSwitchAudio);
        SwitchVideoCommand = new AsyncDelegateCommand<bool>(OnSwitchVideo);
        SwitchMainVideoCommand = new DelegateCommand(SwitchMainVideo);
    }

    #region Command

    private void SwitchMainVideo()
    {
        IsRemoteMainVideo = !IsRemoteMainVideo;
    }

    private async Task OnSwitchVideo(bool isClose)
    {
        IsLocalVideoOpened = !isClose;

        // 发送视频状态改变的消息
        var stateChanged = new VideoStateChanged
        {
            IsOpen = !isClose,
            UserId = _userManager.User!.Id,
            TargetId = UserTarget!.Id
        };
        await _messageHelper.SendMessage(stateChanged);

        if (_videoCallOperator != null)
        {
            await _videoCallOperator.ChangeVideoState(!isClose);
        }
    }

    private async Task OnSwitchAudio(bool isClose)
    {
        // 发送音频状态改变的消息
        var stateChanged = new AudioStateChanged
        {
            IsOpen = !isClose,
            UserId = _userManager.User!.Id,
            TargetId = UserTarget!.Id
        };
        await _messageHelper.SendMessage(stateChanged);

        if (_videoCallOperator != null)
        {
            await _videoCallOperator.ChangeAudioState(!isClose);
        }
    }

    private async Task OnAccept()
    {
        if (_videoCallOperator != null)
            await _videoCallOperator.AcceptCall(UserTarget!.Id);
    }

    private async Task OnHangUp()
    {
        if (state == CallViewState.Calling && !IsSender)
        {
            if (_videoCallOperator != null)
                await _videoCallOperator.RejectCall(UserTarget!.Id);
            RequestClose.Invoke(new DialogResult(ButtonResult.OK));
        }
        else
        {
            if (_cancellationTokenSource != null)
                await _cancellationTokenSource.CancelAsync();

            if (_videoCallOperator != null)
                await _videoCallOperator.EndCall();
            RequestClose.Invoke(new DialogResult(ButtonResult.OK));
        }
    }

    #endregion

    #region Audio

    private void PlayRing()
    {
        if (isAudioOver) return;

        var ringPath = Path.Combine(Environment.CurrentDirectory, "Assets", "ring.mp3");
        _audioPlayer = new AudioPlayer();
        _audioPlayer.LoadFile(ringPath);
        _audioPlayer.Play();
        _audioPlayer.PlaybackCompleted += PlayerAgain;
    }

    private void PlayerAgain(object sender, EventArgs e)
    {
        if (isAudioOver) return;
        if (_audioPlayer == null) return;
        _audioPlayer.CurrentPosition = TimeSpan.Zero;
        _audioPlayer.Play();
    }

    private void StopRing()
    {
        isAudioOver = true;
        if (_audioPlayer == null) return;
        _audioPlayer.PlaybackCompleted -= PlayerAgain;
        _audioPlayer.Stop();
        _audioPlayer.Dispose();
        _audioPlayer = null;
    }


    /// <summary>
    /// 挂断铃声
    /// </summary>
    private void HangUpRing()
    {
        var audio = new AudioPlayer();
        audio.LoadFile(Path.Combine(Environment.CurrentDirectory, "Assets", "hangup.mp3"));
        audio.Play();
        audio.PlaybackCompleted += (s, e) =>
        {
            audio.Stop();
            audio.Dispose();
        };
    }

    #endregion

    #region Video

    /// <summary>
    /// 处理本地视频帧
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnLocalVideoFrameReceived(object? sender, VideoFrameReceivedEventArgs args)
    {
        try
        {
            // 检查帧大小或格式是否变化
            bool needNewBitmap = _localVideoFrame == null ||
                                 _localVideoFrame.PixelSize.Width != args.Width ||
                                 _localVideoFrame.PixelSize.Height != args.Height;

            if (needNewBitmap)
            {
                // 创建新的WriteableBitmap
                _localVideoFrame = new WriteableBitmap(
                    new PixelSize(args.Width, args.Height),
                    new Vector(96, 96),
                    PixelFormat.Bgra8888,
                    AlphaFormat.Opaque);
            }

            VideoBitmapHelper.OperateVideoFrame(args, _localVideoFrame);

            // 强制更新UI - 为了确保视频帧更新
            Dispatcher.UIThread.Invoke(() =>
            {
                var updatedBitmap = _localVideoFrame;
                LocalVideoFrame = null;
                LocalVideoFrame = updatedBitmap;
            }, DispatcherPriority.Render);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            throw;
        }
    }

    /// <summary>
    /// 处理远程视频帧
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnVideoFrameReceived(object? sender, VideoFrameReceivedEventArgs args)
    {
        try
        {
            // 检查帧大小或格式是否变化
            bool needNewBitmap = _remoteVideoFrame == null ||
                                 _remoteVideoFrame.PixelSize.Width != args.Width ||
                                 _remoteVideoFrame.PixelSize.Height != args.Height;

            if (needNewBitmap)
            {
                // 创建新的WriteableBitmap
                _remoteVideoFrame = new WriteableBitmap(
                    new PixelSize(args.Width, args.Height),
                    new Vector(96, 96),
                    PixelFormat.Bgra8888,
                    AlphaFormat.Opaque);
            }

            VideoBitmapHelper.OperateVideoFrame(args, _remoteVideoFrame);

            // 强制更新UI - 为了确保视频帧更新
            Dispatcher.UIThread.Invoke(() =>
            {
                var updatedBitmap = _remoteVideoFrame;
                RemoteVideoFrame = null;
                RemoteVideoFrame = updatedBitmap;
            }, DispatcherPriority.Render);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            throw;
        }
    }

    #endregion

    #region Event

    private void OnConnected()
    {
        _subscriptions.Add(_eventAggregator.GetEvent<ResponseEvent<AudioStateChanged>>().Subscribe(mess =>
        {
            if (mess.TargetId.Equals(_userManager.User!.Id) && mess.UserId.Equals(UserTarget.Id))
                IsRemoteAudioOpened = mess.IsOpen;
        }));

        _subscriptions.Add(_eventAggregator.GetEvent<ResponseEvent<VideoStateChanged>>().Subscribe(async mess =>
        {
            if (mess.TargetId.Equals(_userManager.User!.Id) && mess.UserId.Equals(UserTarget.Id))
                IsRemoteVideoOpened = mess.IsOpen;
        }));
    }

    private void OnDisconnected()
    {
        foreach (var token in _subscriptions)
            token.Dispose();
    }

    #endregion

    #region IDialogAware

    public bool CanCloseDialog() => true;

    public void OnDialogClosed()
    {
        StopRing();

        _remoteVideoFrame = null;
        _localVideoFrame = null;

        // 退出通话
        ClearCall();
        UserTarget = null;
    }

    public async void OnDialogOpened(IDialogParameters parameters)
    {
        State = CallViewState.Calling;

        var callOperator = parameters.GetValue<CallOperatorBase>("callOperator");
        _videoCallOperator = callOperator as VideoCallOperator;
        _videoCallOperator.OnCallStatusChanged += CallStatusChanged;
        _videoCallOperator.OnIceConntectionStateChanged += IceConnectionStateChanged;
        _videoCallOperator.OnVideoFrameReceived += OnVideoFrameReceived;
        _videoCallOperator.OnLocalVideoFrameReceived += OnLocalVideoFrameReceived;

        if (parameters.ContainsKey("request"))
        {
            var callRequest = parameters.GetValue<CallRequest>("request");
            UserTarget = await _userDtoManager.GetFriendRelationDto(callRequest.Callee, callRequest.Caller)!;
            Message = "对方请求视频通话";
            _videoCallOperator.ReceiveCall(userTarget!.Id);

            IsSender = false;
        }
        else if (parameters.ContainsKey("sender"))
        {
            UserTarget = parameters.GetValue<FriendRelationDto>("sender");
            IsSender = true;
            Message = "正在等待对方接听";

            RequestCall();
        }

        // 建立通话之后，监听远端视频状态
        OnConnected();

        // 播放铃声
        PlayRing();
    }

    private async void RequestCall()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        bool result = false;
        try
        {
            result = await _videoCallOperator.InitiateCall(userTarget.Id, _cancellationTokenSource);
            if (!result)
            {
                State = CallViewState.Over;
                Message = "对方拒绝通话请求";

                StopRing();

                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }
        catch (CallException e)
        {
            StopRing();

            State = CallViewState.Over;

            if (e.Type == CallExceptionType.RequestTimeout)
            {
                Message = "请求通话超时";
            }
            else if (e.Type == CallExceptionType.AlreadyInCall)
            {
                Message = "对方已在通话中";
            }
            else if (e.Type == CallExceptionType.OutLine)
            {
                Message = "对方离线中";
            }
        }
    }

    private void CallStatusChanged(object? sender, CallStatus e)
    {
        if (e == CallStatus.InCall)
        {
            if (state == CallViewState.InCall) return;

            Message = "正在建立通话连接...";

            StopRing();

            State = CallViewState.Connecting;
        }
        else if (e == CallStatus.Ended)
        {
            var callManager = _containerProvider.Resolve<ICallManager>();
            callManager.RemoveCall();

            ClearCall();

            if (State == CallViewState.Calling && !IsSender)
            {
                Dispatcher.UIThread.Post(() => { RequestClose.Invoke(new DialogResult(ButtonResult.OK)); });
            }
            else if (State != CallViewState.Calling && State != CallViewState.Over)
            {
                State = CallViewState.Over;
                Message = "通话结束";
            }

            HangUpRing();
        }
    }

    private async void IceConnectionStateChanged(object? sender, RTCIceConnectionState e)
    {
        if (e == RTCIceConnectionState.connected)
        {
            Message = "视频通话中...";

            State = CallViewState.InCall;
        }
        else if (e == RTCIceConnectionState.failed)
        {
            Message = "视频通话连接失败";

            State = CallViewState.Over;

            var callManager = _containerProvider.Resolve<ICallManager>();
            await callManager.RemoveCall();
        }
    }

    private void ClearCall()
    {
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;

        if (_videoCallOperator == null) return;

        _videoCallOperator.OnCallStatusChanged -= CallStatusChanged;
        _videoCallOperator.OnIceConntectionStateChanged -= IceConnectionStateChanged;
        _videoCallOperator.OnVideoFrameReceived -= OnVideoFrameReceived;
        _videoCallOperator.OnLocalVideoFrameReceived -= OnLocalVideoFrameReceived;
        _videoCallOperator = null;

        OnDisconnected();
    }

    public DialogCloseListener RequestClose { get; }

    #endregion
}