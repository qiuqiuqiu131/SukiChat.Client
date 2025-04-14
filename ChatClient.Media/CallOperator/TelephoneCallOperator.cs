using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
using ChatServer.Common.Protobuf;
using Microsoft.Extensions.Configuration;
using SIPSorcery.Media;
using SIPSorcery.Net;
using SIPSorcery.Windows;

namespace ChatClient.Media.CallOperator;

public class TelephoneCallOperator(
    IMessageHelper messageHelper,
    IUserManager userManager,
    IConfigurationRoot configurationRoot)
    : CallOperatorBase(messageHelper, userManager, configurationRoot)
{
    private WindowsAudioEndPoint? _audioEndPoint;

    public event EventHandler<RTCIceConnectionState> OnIceConntectionStateChanged;

    /// <summary>
    /// 更改音频播放状态
    /// </summary>
    /// <param name="isOpen"></param>
    public async Task ChangeAudioState(bool isOpen)
    {
        if (_audioEndPoint == null) return;

        if (isOpen)
                await _audioEndPoint.StartAudio();
        else
            await _audioEndPoint.PauseAudio();
    }

    /// <summary>
    /// 创建PeerConnection
    /// </summary>
    protected async override Task<RTCPeerConnection> CreatePeerConnection()
    {
        var peerConnection = new RTCPeerConnection(RtcConfiguration);

        // 添加媒体轨道
        _audioEndPoint = new WindowsAudioEndPoint(new AudioEncoder());

        // 添加音频轨道
        var audioTrack = new MediaStreamTrack(_audioEndPoint.GetAudioSourceFormats());
        peerConnection.addTrack(audioTrack);
        _audioEndPoint.OnAudioSourceEncodedSample += peerConnection.SendAudio;

        // 添加音频和视频格式协商
        peerConnection.OnAudioFormatsNegotiated += format => _audioEndPoint.SetAudioSourceFormat(format.First());

        // 接受远端视频帧
        peerConnection.OnRtpPacketReceived += (rep, media, rtpPkt) =>
        {
            if (media == SDPMediaTypesEnum.audio)
                _audioEndPoint.GotAudioRtp(rep, rtpPkt.Header.SyncSource, rtpPkt.Header.SequenceNumber,
                    rtpPkt.Header.Timestamp, rtpPkt.Header.PayloadType, rtpPkt.Header.MarkerBit == 1, rtpPkt.Payload);
        };

        // 监听ICE候选生成
        peerConnection.onicecandidate += (candidate) =>
        {
            if (candidate != null)
            {
                // 将ICE候选发送给对方
                var iceMessage = new SignalingMessage
                {
                    Type = SignalingType.IceCandidate,
                    From = _userId,
                    To = _peerId,
                    Ice = new IceCandidate
                    {
                        Candidate = candidate.candidate,
                        SdpMid = candidate.sdpMid ?? "",
                        SdpMLineIndex = candidate.sdpMLineIndex
                    }
                };

                _messageHelper.SendMessage(iceMessage).Wait();
            }
        };

        peerConnection.oniceconnectionstatechange += state =>
        {
            Console.WriteLine($"ICE连接状态: {state}");
            OnIceConntectionStateChanged?.Invoke(this, state);
        };

        // 监听连接状态变化
        peerConnection.onconnectionstatechange += async (state) =>
        {
            if (state == RTCPeerConnectionState.disconnected ||
                state == RTCPeerConnectionState.failed ||
                state == RTCPeerConnectionState.closed)
            {
                await CleanupCall();
            }
            else if (state == RTCPeerConnectionState.connected)
            {
                // 监听远端音频流
                await _audioEndPoint.StartAudioSink();
                await _audioEndPoint.StartAudio();
            }
        };

        return peerConnection;
    }

    /// <summary>
    /// 清理通话资源
    /// </summary>
    protected override async Task CleanupCall()
    {
        await base.CleanupCall();

        if (_audioEndPoint != null)
            await _audioEndPoint.CloseAudio();

        _audioEndPoint = null;
    }

    protected override CallType GetCallType() => CallType.Telephone;
}