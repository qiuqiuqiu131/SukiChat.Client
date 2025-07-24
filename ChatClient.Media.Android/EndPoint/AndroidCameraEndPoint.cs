using System.Net;
using ChatClient.Tool.Media.EndPoint;
using SIPSorceryMedia.Abstractions;

namespace ChatClient.Media.Android.EndPoint;

public class AndroidCameraEndPoint:ICameraEndPoint
{
    public Task PauseVideo()
    {
        throw new NotImplementedException();
    }

    public Task ResumeVideo()
    {
        throw new NotImplementedException();
    }

    public Task StartVideo()
    {
        throw new NotImplementedException();
    }

    public Task CloseVideo()
    {
        throw new NotImplementedException();
    }

    public List<VideoFormat> GetVideoSourceFormats()
    {
        throw new NotImplementedException();
    }

    public void SetVideoSourceFormat(VideoFormat videoFormat)
    {
        throw new NotImplementedException();
    }

    public void GotVideoRtp(IPEndPoint remoteEndPoint, uint ssrc, uint seqnum, uint timestamp, int payloadID, bool marker,
        byte[] payload)
    {
        throw new NotImplementedException();
    }

    public void GotVideoFrame(IPEndPoint remoteEndPoint, uint timestamp, byte[] payload, VideoFormat format)
    {
        throw new NotImplementedException();
    }

    public List<VideoFormat> GetVideoSinkFormats()
    {
        throw new NotImplementedException();
    }

    public void SetVideoSinkFormat(VideoFormat videoFormat)
    {
        throw new NotImplementedException();
    }

    void IVideoSink.RestrictFormats(Func<VideoFormat, bool> filter)
    {
        throw new NotImplementedException();
    }

    public Task PauseVideoSink()
    {
        throw new NotImplementedException();
    }

    public Task ResumeVideoSink()
    {
        throw new NotImplementedException();
    }

    public Task StartVideoSink()
    {
        throw new NotImplementedException();
    }

    public Task CloseVideoSink()
    {
        throw new NotImplementedException();
    }

    public event VideoSinkSampleDecodedDelegate? OnVideoSinkDecodedSample;
    public event VideoSinkSampleDecodedFasterDelegate? OnVideoSinkDecodedSampleFaster;

    void IVideoSource.RestrictFormats(Func<VideoFormat, bool> filter)
    {
        throw new NotImplementedException();
    }

    public void ExternalVideoSourceRawSample(uint durationMilliseconds, int width, int height, byte[] sample,
        VideoPixelFormatsEnum pixelFormat)
    {
        throw new NotImplementedException();
    }

    public void ExternalVideoSourceRawSampleFaster(uint durationMilliseconds, RawImage rawImage)
    {
        throw new NotImplementedException();
    }

    public void ForceKeyFrame()
    {
        throw new NotImplementedException();
    }

    public bool HasEncodedVideoSubscribers()
    {
        throw new NotImplementedException();
    }

    public bool IsVideoSourcePaused()
    {
        throw new NotImplementedException();
    }

    public event EncodedSampleDelegate? OnVideoSourceEncodedSample;
    public event RawVideoSampleDelegate? OnVideoSourceRawSample;
    public event RawVideoSampleFasterDelegate? OnVideoSourceRawSampleFaster;
    public event SourceErrorDelegate? OnVideoSourceError;
    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public MediaEndPoints ToMediaEndPoints()
    {
        throw new NotImplementedException();
    }

    public bool IsRunning { get; }
}