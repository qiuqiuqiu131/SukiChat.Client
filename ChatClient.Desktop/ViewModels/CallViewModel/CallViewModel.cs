using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Notification;
using Avalonia.Threading;
using ChatClient.BaseService.Manager;
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

public class CallViewModel : BindableBase, IDialogAware, ICallView
{
    private readonly IUserDtoManager _userDtoManager;
    private readonly IUserManager _userManager;
    private readonly IContainerProvider _containerProvider;
    private readonly IMessageHelper _messageHelper;
    private readonly IEventAggregator _eventAggregator;

    private List<SubscriptionToken> _subscriptions = [];

    private TelephoneCallOperator? _telephoneCallOperator;

    private CancellationTokenSource? _cancellationTokenSource;

    private AudioPlayer? _audioPlayer;

    private bool isAudioOver;

    public NotificationMessageManager NotificationMessageManager { get; set; } = new NotificationMessageManager();

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

    private bool isRemoteAudioOpened;

    public bool IsRemoteAudioOpened
    {
        get => isRemoteAudioOpened;
        set => SetProperty(ref isRemoteAudioOpened, value);
    }

    private bool isFailed = true;

    private DateTime? callStartTime;

    public AsyncDelegateCommand HangUpCommand { get; set; }
    public AsyncDelegateCommand AcceptCommand { get; set; }
    public AsyncDelegateCommand<bool> SwitchAudioCommand { get; set; }

    public CallViewModel(IUserDtoManager userDtoManager, IUserManager userManager, IContainerProvider containerProvider,
        IMessageHelper messageHelper,
        IEventAggregator eventAggregator)
    {
        _userDtoManager = userDtoManager;
        _userManager = userManager;
        _containerProvider = containerProvider;
        _messageHelper = messageHelper;
        _eventAggregator = eventAggregator;

        HangUpCommand = new AsyncDelegateCommand(OnHangUp);
        AcceptCommand = new AsyncDelegateCommand(OnAccept);
        SwitchAudioCommand = new AsyncDelegateCommand<bool>(OnSwitchAudio);
    }

    #region Command

    private async Task OnSwitchAudio(bool isClose)
    {
        if (_telephoneCallOperator != null)
            await _telephoneCallOperator.ChangeAudioState(!isClose);

        // 发送远端音频状态
        var audioState = new AudioStateChanged
        {
            IsOpen = !isClose,
            UserId = _userManager.User!.UserDto.Id,
            TargetId = UserTarget!.Id
        };
        await _messageHelper.SendMessage(audioState);
    }

    private async Task OnAccept()
    {
        if (_telephoneCallOperator != null)
            await _telephoneCallOperator.AcceptCall(UserTarget!.Id);
    }

    private async Task OnHangUp()
    {
        if (state == CallViewState.Calling && !IsSender)
        {
            if (_telephoneCallOperator != null)
                await _telephoneCallOperator.RejectCall(UserTarget!.Id);
            RequestClose.Invoke(new DialogResult(ButtonResult.OK));
        }
        else
        {
            if (_cancellationTokenSource != null)
                await _cancellationTokenSource.CancelAsync();

            if (_telephoneCallOperator != null)
                await _telephoneCallOperator.EndCall();
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
        _audioPlayer.Stop();
        _audioPlayer.PlaybackCompleted -= PlayerAgain;
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

    #region Event

    private void OnConnected()
    {
        _subscriptions.Add(_eventAggregator.GetEvent<ResponseEvent<AudioStateChanged>>().Subscribe(mess =>
        {
            if (mess.TargetId.Equals(_userManager.User!.UserDto.Id) && mess.UserId.Equals(UserTarget!.Id))
                IsRemoteAudioOpened = mess.IsOpen;
        }));
    }

    private void OnDisConnected()
    {
        foreach (var token in _subscriptions)
            token.Dispose();
    }

    #endregion

    #region IDialogAware

    public bool CanCloseDialog() => true;

    public void OnDialogClosed()
    {
        // 退出通话
        ClearCall();
        StopRing();
        UserTarget = null;
    }

    public async void OnDialogOpened(IDialogParameters parameters)
    {
        State = CallViewState.Calling;

        var callOperator = parameters.GetValue<CallOperatorBase>("callOperator");
        _telephoneCallOperator = callOperator as TelephoneCallOperator;
        _telephoneCallOperator.OnCallStatusChanged += CallStatusChanged;
        _telephoneCallOperator.OnIceConntectionStateChanged += IceConnectionStateChanged;

        if (parameters.ContainsKey("request"))
        {
            var callRequest = parameters.GetValue<CallRequest>("request");
            UserTarget = await _userDtoManager.GetFriendRelationDto(callRequest.Callee, callRequest.Caller)!;
            Message = "对方请求语音通话";
            _telephoneCallOperator.ReceiveCall(userTarget!.Id);

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
            result = await _telephoneCallOperator.InitiateCall(userTarget.Id, _cancellationTokenSource);
            if (!result)
            {
                State = CallViewState.Over;
                Message = "对方拒绝通话请求";

                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                StopRing();
                HangUpRing();

                var callManager = _containerProvider.Resolve<ICallManager>();
                await callManager.RemoveCall();
            }
        }
        catch (CallException e)
        {
            State = CallViewState.Over;

            StopRing();

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

            var callManager = _containerProvider.Resolve<ICallManager>();
            await callManager.RemoveCall();
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
                if (callStartTime == null)
                    Message = "通话结束";
                else
                {
                    var minute = (int)(DateTime.Now - callStartTime)!.Value.TotalMinutes;
                    var second = (int)(DateTime.Now - callStartTime)!.Value.TotalSeconds % 60;
                    Message = $"通话结束 {minute:D2}:{second:D2}";
                }

                HangUpRing();
            }

            if (IsSender)
            {
                _eventAggregator.GetEvent<CallOver>().Publish(new CallMessDto
                {
                    targetId = peerId,
                    IsUser = true,
                    CallTime = callStartTime == null ? 0 : (int)(DateTime.Now - callStartTime)!.Value.TotalSeconds,
                    IsTelephone = true,
                    Failed = isFailed
                });
            }
        }
    }

    private CancellationTokenSource? checkingCancellationTokenSource;

    private async void IceConnectionStateChanged(object? sender, RTCIceConnectionState e)
    {
        if (e == RTCIceConnectionState.connected)
        {
            Message = "语音通话中...";

            State = CallViewState.InCall;

            checkingCancellationTokenSource?.CancelAsync();
            checkingCancellationTokenSource = null;

            callStartTime = DateTime.Now;
            isFailed = false;
        }
        else if (e == RTCIceConnectionState.failed)
        {
            Message = "语音通话连接失败";

            State = CallViewState.Over;

            var callManager = _containerProvider.Resolve<ICallManager>();
            await callManager.RemoveCall();
        }
        else if (e == RTCIceConnectionState.checking)
        {
            checkingCancellationTokenSource?.CancelAsync();
            if (State == CallViewState.InCall)
                return;

            checkingCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            // 添加超时处理逻辑
            Task.Delay(TimeSpan.FromSeconds(5), checkingCancellationTokenSource.Token).ContinueWith(async t =>
            {
                if (t.IsCanceled)
                    return; // 如果任务被取消（即连接成功或主动取消），就不执行后续操作

                // 如果到这里，说明超时了且任务未被取消
                Message = "语音通话连接失败";
                State = CallViewState.Over;

                var callManager = _containerProvider.Resolve<ICallManager>();
                await callManager.RemoveCall();
            });
        }
    }

    private void ClearCall()
    {
        StopRing();

        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;

        if (_telephoneCallOperator == null) return;

        _telephoneCallOperator.OnCallStatusChanged -= CallStatusChanged;
        _telephoneCallOperator.OnIceConntectionStateChanged -= IceConnectionStateChanged;
        _telephoneCallOperator = null;

        OnDisConnected();
    }

    public DialogCloseListener RequestClose { get; }

    #endregion
}

public enum CallViewState
{
    Calling,
    Connecting,
    InCall,
    Over
}