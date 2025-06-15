using SIPSorceryMedia.Abstractions;

namespace ChatClient.Media.EndPoint;

public interface ICameraEndPoint : IVideoSource, IVideoSink, IDisposable
{
    MediaEndPoints ToMediaEndPoints();

    bool IsRunning { get; }
}