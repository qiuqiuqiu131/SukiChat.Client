using System.Net;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
using ChatClient.Tool.Media.Call;
using ChatServer.Common.Protobuf;
using Microsoft.Extensions.Configuration;
using SIPSorcery.Net;

namespace ChatClient.Media.CallOperator;

public abstract class CallOperatorBase : ICallSession, ICallOperator, IDisposable
{
    protected readonly IMessageHelper _messageHelper;
    private readonly IStunServerManager _stunServerManager;
    private readonly IConfigurationRoot _configurationRoot;
    private readonly IUserSetting _userSetting;

    protected RTCPeerConnection? _peerConnection;

    // 当前用户ID
    protected string _userId;

    // 当前通话对象ID
    protected string _peerId;

    // 是否正在通话
    protected bool _isInCall = false;

    // 通话状态发生变化
    public event EventHandler<CallStatus> OnCallStatusChanged;

    public CallOperatorBase(IMessageHelper messageHelper, IUserManager userManager,
        IStunServerManager stunServerManager, IConfigurationRoot configurationRoot, IUserSetting userSetting)
    {
        _messageHelper = messageHelper;
        _configurationRoot = configurationRoot;
        _userSetting = userSetting;
        _stunServerManager = stunServerManager;
        _userId = userManager.User!.Id;
    }


    #region ICallSession

    /// <summary>
    /// 发起通话请求
    /// </summary>
    /// <param name="peerId">对方ID</param>
    /// <returns>
    /// bool :如果对方拒绝将会返回false
    /// 请使用try-catch捕获CallExceprion异常
    /// </returns>
    public async Task<bool> InitiateCall(string peerId, CancellationTokenSource? cancellationToken)
    {
        if (_isInCall)
        {
            throw new CallException(CallExceptionType.AlreadyInCall, "已经在通话中");
        }

        _peerId = peerId;
        OnCallStatusChanged?.Invoke(this, CallStatus.Calling);


        var callRequest = new CallRequest
        {
            Caller = _userId,
            Callee = peerId,
            Type = GetCallType()
        };

        var callResponse =
            await _messageHelper.SendMessageWithResponse<CallResponse>(callRequest, TimeSpan.FromMinutes(1),
                cancellationToken);

        if (callResponse == null)
            throw new CallException(CallExceptionType.RequestTimeout, "请求通话超时");

        if (callResponse is not { Response: { State: true } })
            throw new CallException(CallExceptionType.OutLine, "对方离线中");

        if (!callResponse.Accept)
            return false;

        SendOffer(peerId);
        return true;
    }

    private async void SendOffer(string peerId)
    {
        // 对方接受通话请求
        // 创建PeerConnection
        _peerConnection = await CreatePeerConnection();

        var offer = _peerConnection.createOffer();
        await _peerConnection.setLocalDescription(offer);

        // 发送offer
        var offerSignalMessage = new SignalingMessage
        {
            Type = SignalingType.Offer,
            From = _userId,
            To = peerId,
            Sdp = offer.sdp
        };

        await _messageHelper.SendMessage(offerSignalMessage);
    }

    public void ReceiveCall(string callerId)
    {
        _peerId = callerId;
    }

    /// <summary>
    /// 接听呼叫
    /// </summary>
    /// <param name="callerId"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<bool> AcceptCall(string callerId)
    {
        if (_isInCall)
        {
            throw new CallException(CallExceptionType.AlreadyInCall, "已经在通话中");
        }

        _peerId = callerId;

        // 创建PeerConnection
        _peerConnection = await CreatePeerConnection();

        var callResponse = new CallResponse
        {
            Response = new CommonResponse { State = true },
            Caller = callerId,
            Callee = _userId,
            Accept = true
        };

        await _messageHelper.SendMessage(callResponse);
        return true;
    }

    public async Task RejectCall(string callerId)
    {
        var callResponse = new CallResponse
        {
            Response = new CommonResponse { State = true, Message = "Decline" },
            Caller = callerId,
            Callee = _userId,
            Accept = false
        };

        await _messageHelper.SendMessage(callResponse);

        await CleanupCall();
    }

    public async Task EndCall()
    {
        var hangupMessage = new HangUp
        {
            From = _userId,
            To = _peerId
        };

        await _messageHelper.SendMessage(hangupMessage);

        await CleanupCall();
    }

    #endregion

    #region ICallOperator

    public async Task HandleOffer(string from, string sdp)
    {
        _peerId = from;

        if (_peerConnection != null)
            _peerConnection = await CreatePeerConnection();

        var offerDescription = new RTCSessionDescriptionInit
        {
            type = RTCSdpType.offer,
            sdp = sdp
        };

        _peerConnection.setRemoteDescription(offerDescription);

        // 创建answer
        var answer = _peerConnection.createAnswer();
        await _peerConnection.setLocalDescription(answer);

        // 发送answer
        var answerSignalMessage = new SignalingMessage
        {
            Type = SignalingType.Answer,
            From = _userId,
            To = from,
            Sdp = answer.sdp
        };

        await _messageHelper.SendMessage(answerSignalMessage);

        OnCallStatusChanged?.Invoke(this, CallStatus.InCall);
        _isInCall = true;
    }

    public async Task HandleAnswer(string from, string sdp)
    {
        if (_peerConnection == null)
        {
            _peerConnection = await CreatePeerConnection();
        }

        var answerDescription = new RTCSessionDescriptionInit
        {
            type = RTCSdpType.answer,
            sdp = sdp
        };

        _peerConnection.setRemoteDescription(answerDescription);

        OnCallStatusChanged.Invoke(this, CallStatus.InCall);
        _isInCall = true;
    }

    public async Task HandleIceCandidate(string from, IceCandidate iceCandidate)
    {
        Console.WriteLine($"接收到ICE候选:{iceCandidate.Candidate}");

        var candidate = new RTCIceCandidateInit
        {
            candidate = iceCandidate.Candidate,
            sdpMid = iceCandidate.SdpMid ?? "",
            sdpMLineIndex = (ushort)iceCandidate.SdpMLineIndex
        };

        try
        {
            _peerConnection?.addIceCandidate(candidate);
        }
        catch (Exception e)
        {
            // 处理异常
            Console.WriteLine($"Error adding ICE candidate: {e.Message}");
        }
    }

    public async Task<bool> HandleHangup(string from)
    {
        if (_peerId != from) return false;

        await CleanupCall();
        return true;
    }

    #endregion

    protected async Task<RTCConfiguration> GetRtcConfiguration()
    {
        var urls = await _stunServerManager.GetStunServersUrl();
        var TransportRelay = _userSetting.UseTurnServer;
        var GatherTime = _configurationRoot.GetSection("GatherTime").Get<int?>() ?? 2000;
        var configuration = new RTCConfiguration
        {
            iceServers = urls.Select(d => new RTCIceServer { urls = d }).ToList(),
            bundlePolicy = RTCBundlePolicy.balanced,
            iceTransportPolicy = TransportRelay ? RTCIceTransportPolicy.relay : RTCIceTransportPolicy.all,
            X_GatherTimeoutMs = GatherTime
        };

        // 添加TURN服务器
        var turnServer = new RTCIceServer
        {
            urls = _configurationRoot["TurnServer:Url"] ?? "turn:120.26.166.13:3478",
            username = _configurationRoot["TurnServer:Username"] ?? "qiuqiuqiu",
            credential = _configurationRoot["TurnServer:Credential"] ?? "hongqiuyang13157"
        };
        configuration.iceServers.Add(turnServer);

        return configuration;
    }


    protected abstract Task<RTCPeerConnection> CreatePeerConnection();

    protected virtual async Task CleanupCall()
    {
        _isInCall = false;

        if (_peerConnection != null)
        {
            _peerConnection.Close("Call ended");
            _peerConnection = null;
        }

        OnCallStatusChanged?.Invoke(this, CallStatus.Ended);
    }

    protected abstract CallType GetCallType();

    public void Dispose()
    {
        _peerConnection?.Dispose();
        CleanupCall().Wait();
    }
}