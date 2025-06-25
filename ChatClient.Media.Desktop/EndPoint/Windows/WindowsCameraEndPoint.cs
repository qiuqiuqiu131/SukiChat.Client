using System.Drawing;
using System.Net;
using AForge;
using AForge.Video;
using AForge.Video.DirectShow;
using ChatClient.Tool.Media.Video;
using Microsoft.Extensions.Logging;
using SIPSorceryMedia.Abstractions;

namespace ChatClient.Media.Desktop.EndPoint.Windows;

/// <summary>
/// 使用AForge.NET实现的视频端点类，用于WebRTC摄像头捕获
/// </summary>
internal class WindowsCameraEndPoint : ICameraEndPoint
{
    private const int VIDEO_SAMPLING_RATE = 90000;
    private const int DEFAULT_FRAMES_PER_SECOND = 30;

    private static ILogger logger = SIPSorcery.LogFactory.CreateLogger<WindowsCameraEndPoint>();

    // 视频格式管理器，用于处理视频格式的选择和转换
    private readonly MediaFormatManager<VideoFormat> _videoFormatManager;
    private readonly IVideoEncoder _videoEncoder;

    private bool _forceKeyFrame = false;

    private DeviceStatus _deviceStatus = DeviceStatus.NotReady;

    private readonly FilterInfoCollection _videoDevices;

    // 视频捕获设备实例
    private readonly string _videoDeviceID;
    private VideoCaptureDevice? _videoSource;

    private int _width = 0;
    private int _height = 0;
    private int _fps = 0;

    private DateTime _lastFrameAt = DateTime.MinValue;

    private bool inited = false;
    private int actualDeviceIndex = 0;
    private VideoCapabilities? _seoCapabilities;


    /// <summary>
    /// 创建一个从本地视频捕获设备获取视频的 AforgeCameraEndPoint
    /// </summary>
    /// <param name="videoEncoder">用于编码和解码视频帧的视频编码器</param>
    /// <param name="videoDeviceID">可选。视频设备ID或名称</param>
    /// <param name="width">可选。视频帧宽度</param>
    /// <param name="height">可选。视频帧高度</param>
    public WindowsCameraEndPoint(
        IVideoEncoder videoEncoder,
        string videoDeviceID,
        int width = 0,
        int height = 0)
    {
        _videoEncoder = videoEncoder;
        _videoDeviceID = videoDeviceID;
        _width = width;
        _height = height;

        _videoFormatManager = new MediaFormatManager<VideoFormat>(videoEncoder.SupportedFormats);
        _videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
    }

    public MediaEndPoints ToMediaEndPoints()
    {
        return new MediaEndPoints
        {
            VideoSource = this,
            VideoSink = this
        };
    }

    public bool IsRunning => _videoSource?.IsRunning ?? false;

    /// <summary>
    /// 初始化视频捕获设备。理想情况下应在尝试使用设备前调用此方法。
    /// </summary>
    /// <returns>如果本地视频捕获设备已成功初始化则为True。否则为False。</returns>
    public async Task<bool> InitialiseVideoSourceDevice()
    {
        if (_deviceStatus == DeviceStatus.NotReady || _videoSource == null)
        {
            bool result = await InitialiseDevice(_width, _height);
            _deviceStatus = result ? DeviceStatus.Ready : DeviceStatus.NotReady;
            return result;
        }

        return true;
    }

    /// <summary>
    /// 尝试初始化本地视频捕获设备
    /// </summary>
    private Task<bool> InitialiseDevice(int width, int height)
    {
        try
        {
            if (_videoDevices.Count == 0)
            {
                logger.LogWarning("没有找到视频捕获设备");
                return Task.FromResult(false);
            }

            // 匹配视频设备ID,如果未提供则选择第一个设备
            int deviceIndex = 0;
            if (inited)
                deviceIndex = actualDeviceIndex;
            else
            {
                if (!string.IsNullOrEmpty(_videoDeviceID))
                {
                    for (int i = 0; i < _videoDevices.Count; i++)
                    {
                        if (_videoDevices[i].MonikerString == _videoDeviceID || _videoDevices[i].Name == _videoDeviceID)
                        {
                            deviceIndex = i;
                            logger.LogInformation($"视频捕获设备 {_videoDevices[i].Name} 已选择");
                            break;
                        }
                    }
                }

                actualDeviceIndex = deviceIndex;
            }

            // 创建视频捕获设备实例
            _videoSource = new VideoCaptureDevice(_videoDevices[deviceIndex].MonikerString);

            // 注册新帧处理事件
            _videoSource.NewFrame += VideoSource_NewFrame;

            if (inited)
            {
                if (_seoCapabilities != null)
                    _videoSource.VideoResolution = _seoCapabilities;
            }
            else
            {
                var seoCapabilities = _videoSource.VideoCapabilities;
                // 设置所需的视频分辨率和帧率
                if (_width > 0 && _height > 0)
                {
                    // 获取视频捕获设备实例的支持分辨率,尝试找到最接近请求分辨率的视频能力
                    var videoCapabilities = _videoSource.VideoCapabilities;
                    if (videoCapabilities != null && videoCapabilities.Length > 0)
                    {
                        VideoCapabilities? bestMatch = null;
                        int minDiff = int.MaxValue;

                        foreach (var capability in videoCapabilities)
                        {
                            int diff = Math.Abs(capability.FrameSize.Width - width) +
                                       Math.Abs(capability.FrameSize.Height - height);

                            if (diff < minDiff)
                            {
                                minDiff = diff;
                                bestMatch = capability;
                            }
                        }

                        if (bestMatch != null)
                        {
                            _seoCapabilities = bestMatch;
                            _videoSource.VideoResolution = bestMatch;
                            _width = bestMatch.FrameSize.Width;
                            _height = bestMatch.FrameSize.Height;
                            // AForge不允许直接设置fps，但可以从capabilities中获取
                            _fps = bestMatch.AverageFrameRate > 0
                                ? bestMatch.AverageFrameRate
                                : DEFAULT_FRAMES_PER_SECOND;

                            logger.LogInformation($"视频捕获设备已初始化: {_width}x{_height} {_fps}fps");
                        }
                    }
                }
            }

            inited = true;
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            logger.LogError($"初始化视频设备失败: {ex.Message}");
            OnVideoSourceError?.Invoke(ex.Message);
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// 处理从摄像头来的新视频帧
    /// </summary>
    private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
    {
        if (_deviceStatus != DeviceStatus.Running || _videoFormatManager.SelectedFormat.IsEmpty())
            return;

        try
        {
            // 获取当前帧
            // var bitmap = (Bitmap)eventArgs.Frame.Clone();
            using (var bitmap = eventArgs.Frame)
            {
                int width = bitmap.Width;
                int height = bitmap.Height;

                // 将位图转换为BGR格式字节数组
                byte[] bgrBuffer = BitmapToByteArray(bitmap);

                // 计算自上一帧以来的时间
                uint frameSpacing = 0;
                if (_lastFrameAt != DateTime.MinValue)
                    frameSpacing = Convert.ToUInt32(DateTime.Now.Subtract(_lastFrameAt).TotalMilliseconds);
                _lastFrameAt = DateTime.Now;

                // 发送原始视频样本事件
                OnVideoSourceRawSample?.Invoke(frameSpacing, width, height, bgrBuffer, VideoPixelFormatsEnum.Bgr);

                // 编码并发送编码后的视频样本
                lock (_videoEncoder)
                {
                    var encodedBuffer = _videoEncoder.EncodeVideo(
                        width,
                        height,
                        bgrBuffer,
                        VideoPixelFormatsEnum.Bgr,
                        _videoFormatManager.SelectedFormat.Codec);

                    if (encodedBuffer != null)
                    {
                        uint durationRtpTS =
                            (uint)(VIDEO_SAMPLING_RATE / (_fps > 0 ? _fps : DEFAULT_FRAMES_PER_SECOND));
                        OnVideoSourceEncodedSample?.Invoke(durationRtpTS, encodedBuffer);
                    }

                    if (_forceKeyFrame)
                    {
                        _forceKeyFrame = false;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"处理视频帧时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 将Bitmap转换为BGR字节数组
    /// </summary>
    private byte[] BitmapToByteArray(Bitmap bitmap)
    {
        int width = bitmap.Width;
        int height = bitmap.Height;
        int stride = width * 3; // BGR格式，每像素3字节
        byte[] buffer = new byte[stride * height];

        System.Drawing.Imaging.BitmapData bmpData = bitmap.LockBits(
            new Rectangle(0, 0, width, height),
            System.Drawing.Imaging.ImageLockMode.ReadOnly,
            System.Drawing.Imaging.PixelFormat.Format24bppRgb);

        try
        {
            IntPtr ptr = bmpData.Scan0;
            int bytes = bmpData.Stride * height;

            // 复制位图数据
            System.Runtime.InteropServices.Marshal.Copy(ptr, buffer, 0, bytes);

            // 如果stride不等于width*3，需要调整(去掉填充字节)
            if (bmpData.Stride != stride)
            {
                byte[] adjustedBuffer = new byte[stride * height];
                for (int y = 0; y < height; y++)
                {
                    Array.Copy(buffer, y * bmpData.Stride, adjustedBuffer, y * stride, stride);
                }

                return adjustedBuffer;
            }

            return buffer;
        }
        finally
        {
            bitmap.UnlockBits(bmpData);
        }
    }

    /// <summary>
    /// 关闭视频捕获设备
    /// </summary>
    /// <returns></returns>
    private Task CloseVideoCaptureDevice()
    {
        try
        {
            if (_videoSource != null)
            {
                _videoSource.NewFrame -= VideoSource_NewFrame;
                _videoSource.SignalToStop();
                // _videoSource.WaitForStop();
            }

            _videoSource = null;
        }
        catch (Exception e)
        {
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 获取系统中可用的视频捕获设备列表
    /// </summary>
    public static List<VideoCaptureDeviceInfo> GetVideoCaptureDevices()
    {
        var devices = new List<VideoCaptureDeviceInfo>();
        try
        {
            var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            if (videoDevices != null && videoDevices.Count > 0)
            {
                foreach (FilterInfo device in videoDevices)
                {
                    devices.Add(new VideoCaptureDeviceInfo
                    {
                        ID = device.MonikerString,
                        Name = device.Name
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取视频设备列表失败: {ex.Message}");
        }

        return devices;
    }

    public void Dispose()
    {
        CloseVideoCaptureDevice();
        _videoEncoder.Dispose();
    }

    #region IVideoSource

    /// <summary>
    /// 当原始视频帧可用时触发此事件
    /// </summary>
    public event RawVideoSampleDelegate OnVideoSourceRawSample;

    // 不用这个
    public event RawVideoSampleFasterDelegate OnVideoSourceRawSampleFaster;

    /// <summary>
    /// 当编码后的视频帧可用时触发此事件
    /// </summary>
    public event EncodedSampleDelegate OnVideoSourceEncodedSample;

    /// <summary>
    /// 如果获取捕获设备时出现问题，将触发此事件
    /// </summary>
    public event SourceErrorDelegate OnVideoSourceError;

    public Task PauseVideo()
    {
        return CloseVideo();
    }

    public Task ResumeVideo()
    {
        return StartVideo();
    }

    public async Task StartVideo()
    {
        if (_videoSource == null)
            await InitialiseVideoSourceDevice();

        if (_videoSource is { IsRunning: false })
        {
            try
            {
                _videoSource.Start();
                logger.LogInformation("摄像头启动成功");
                _deviceStatus = DeviceStatus.Running;
            }
            catch (Exception ex)
            {
                _deviceStatus = DeviceStatus.NotReady;
                logger.LogError($"启动摄像头失败: {ex.Message}");
                throw;
            }
        }
        else if (_videoSource == null)
        {
            _deviceStatus = DeviceStatus.NotReady;
            logger.LogWarning("无法启动摄像头：未初始化视频源");
            throw new DeviceErrorException("摄像头未初始化或已关闭");
        }
    }

    public Task CloseVideo()
    {
        if (_videoSource == null)
            return Task.CompletedTask;

        _deviceStatus = DeviceStatus.NotReady;
        CloseVideoCaptureDevice();
        return Task.CompletedTask;
    }

    public List<VideoFormat> GetVideoSourceFormats() => _videoFormatManager.GetSourceFormats();
    public void SetVideoSourceFormat(VideoFormat videoFormat) => _videoFormatManager.SetSelectedFormat(videoFormat);

    public void ExternalVideoSourceRawSample(uint durationMilliseconds, int width, int height, byte[] sample,
        VideoPixelFormatsEnum pixelFormat) =>
        throw new ApplicationException("AForge Video End Point does not support external samples.");

    public void ExternalVideoSourceRawSampleFaster(uint durationMilliseconds, RawImage rawImage) =>
        throw new ApplicationException("AForge Video End Point does not support external samples.");

    public void ForceKeyFrame() => _forceKeyFrame = true;

    public bool HasEncodedVideoSubscribers() => OnVideoSourceEncodedSample != null;
    public bool IsVideoSourcePaused() => _deviceStatus == DeviceStatus.Paused;

    #endregion

    #region IVideoSink

    public List<VideoFormat> GetVideoSinkFormats() => _videoFormatManager.GetSourceFormats();
    public void SetVideoSinkFormat(VideoFormat videoFormat) => _videoFormatManager.SetSelectedFormat(videoFormat);
    public void RestrictFormats(Func<VideoFormat, bool> filter) => _videoFormatManager.RestrictFormats(filter);

    /// <summary>
    /// 当从远程方解码视频帧后触发此事件
    /// </summary>
    public event VideoSinkSampleDecodedDelegate OnVideoSinkDecodedSample;

    public event VideoSinkSampleDecodedFasterDelegate OnVideoSinkDecodedSampleFaster;

    public void GotVideoRtp(IPEndPoint remoteEndPoint, uint ssrc, uint seqnum, uint timestamp, int payloadID,
        bool marker, byte[] payload) =>
        throw new ApplicationException(
            "AForge Video End Point requires full video frames rather than individual RTP packets.");

    public void GotVideoFrame(IPEndPoint remoteEndPoint, uint timestamp, byte[] frame, VideoFormat format)
    {
        var decodedFrames = _videoEncoder.DecodeVideo(frame, VideoPixelFormatsEnum.Bgr,
            _videoFormatManager.SelectedFormat.Codec);

        if (decodedFrames == null)
        {
            logger.LogWarning("解码视频样本失败");
        }
        else
        {
            foreach (var decodedFrame in decodedFrames)
            {
                OnVideoSinkDecodedSample?.Invoke(
                    decodedFrame.Sample,
                    decodedFrame.Width,
                    decodedFrame.Height,
                    (int)(decodedFrame.Width * 3),
                    VideoPixelFormatsEnum.Bgr);
            }
        }
    }

    public Task PauseVideoSink() => Task.CompletedTask;
    public Task ResumeVideoSink() => Task.CompletedTask;
    public Task StartVideoSink() => Task.CompletedTask;
    public Task CloseVideoSink() => Task.CompletedTask;

    #endregion
}