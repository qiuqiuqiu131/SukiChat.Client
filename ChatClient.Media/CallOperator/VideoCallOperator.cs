using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
using ChatServer.Common.Protobuf;
using SIPSorcery.Media;
using SIPSorcery.Net;
using SIPSorcery.Windows;
using SIPSorceryMedia.Encoders;

namespace ChatClient.Media.CallOperator;

public class VideoCallOperator(IMessageHelper messageHelper, IUserManager userManager)
    : CallOperatorBase(messageHelper, userManager)
{
    private bool isCameraOpen;
    private WindowsCameraEndPoint? _cameraEndPoint;

    private bool isAudioOpen;
    private WindowsAudioEndPoint? _audioEndPoint;

    // -- 事件 -- //
    // 接收到远端视频帧
    public event EventHandler<VideoFrameReceivedEventArgs>? OnVideoFrameReceived;

    // 接收到本地视频帧
    public event EventHandler<VideoFrameReceivedEventArgs>? OnLocalVideoFrameReceived;

    public event EventHandler<RTCIceConnectionState> OnIceConntectionStateChanged;

    /// <summary>
    /// 更改视频播放状态
    /// </summary>
    /// <param name="isOpen"></param>
    public async Task ChangeVideoState(bool isOpen)
    {
        if (_cameraEndPoint == null)
        {
            Console.WriteLine("摄像头未初始化");
            return;
        }

        if (isOpen)
        {
            if (isCameraOpen)
            {
                await _cameraEndPoint.ResumeVideo();
            }
            else
            {
                isCameraOpen = true;
                bool initialized = await _cameraEndPoint.InitialiseVideoSourceDevice();
                if (initialized)
                {
                    await _cameraEndPoint.StartVideo();
                }
                else
                {
                    Console.WriteLine("摄像头初始化失败");
                    isCameraOpen = false;
                }
            }
        }
        else
            await _cameraEndPoint.PauseVideo();
    }

    /// <summary>
    /// 更改音频播放状态
    /// </summary>
    /// <param name="isOpen"></param>
    public async Task ChangeAudioState(bool isOpen)
    {
        if (_audioEndPoint == null) return;

        if (isOpen)
        {
            if (isAudioOpen)
                await _audioEndPoint.ResumeAudio();
            else
            {
                isAudioOpen = true;
                await _audioEndPoint.StartAudio();
            }
        }
        else
            await _audioEndPoint.PauseAudio();
    }

    /// <summary>
    /// 创建PeerConnection
    /// </summary>
    protected override async Task<RTCPeerConnection> CreatePeerConnection()
    {
        var peerConnection = new RTCPeerConnection(RtcConfiguration);

        // 添加媒体轨道
        // IVideoSource videoEndPoint = _currentVideoSource == VideoSourceType.Camera ? _cameraEndPoint : _screenEndPoint;
        _audioEndPoint = new WindowsAudioEndPoint(new AudioEncoder());
        _cameraEndPoint =
            new WindowsCameraEndPoint(new VpxVideoEncoder(), WindowsCameraEndPoint.GetVideoCaptureDevices()[0].ID);


        // 添加音频轨道
        var audioTrack = new MediaStreamTrack(_audioEndPoint.GetAudioSourceFormats());
        peerConnection.addTrack(audioTrack);
        _audioEndPoint.OnAudioSourceEncodedSample += peerConnection.SendAudio;

        // 添加视频轨道
        var videoTrack = new MediaStreamTrack(_cameraEndPoint.GetVideoSourceFormats());
        peerConnection.addTrack(videoTrack);
        _cameraEndPoint.OnVideoSourceRawSample += (milliseconds, width, height, sample, format) =>
        {
            OnLocalVideoFrameReceived?.Invoke(this, new VideoFrameReceivedEventArgs
            {
                IsCamera = true,
                FrameData = sample,
                Height = height,
                Width = width,
                PixelFormat = format
            });
        };
        _cameraEndPoint.OnVideoSourceEncodedSample += (units, sample) => peerConnection.SendVideo(units, sample);
        _cameraEndPoint.OnVideoSinkDecodedSample += (s, w, h, st, p) =>
        {
            OnVideoFrameReceived?.Invoke(this, new VideoFrameReceivedEventArgs
            {
                IsCamera = true,
                Width = (int)w,
                Height = (int)h,
                FrameData = s,
                PixelFormat = p
            });
        };

        // 添加音频和视频格式协商
        peerConnection.OnAudioFormatsNegotiated += format => _audioEndPoint.SetAudioSourceFormat(format.First());
        peerConnection.OnVideoFormatsNegotiated += format => _cameraEndPoint.SetVideoSourceFormat(format.First());

        // 接受远端视频帧
        peerConnection.OnVideoFrameReceived += _cameraEndPoint.GotVideoFrame;
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
                await _audioEndPoint.StartAudioSink();
                await _audioEndPoint.StartAudio();
                // await _cameraEndPoint.StartVideo();
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
        if (_cameraEndPoint != null)
            await _cameraEndPoint.CloseVideo();

        _audioEndPoint = null;
        _cameraEndPoint = null;
    }

    protected override CallType GetCallType() => CallType.Video;
}