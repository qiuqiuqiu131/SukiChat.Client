using SIPSorceryMedia.Abstractions;

namespace ChatClient.Tool.Media.EndPoint;

public interface ICameraEndPoint : IVideoSource, IVideoSink, IDisposable
{
    MediaEndPoints ToMediaEndPoints();

    bool IsRunning { get; }
}