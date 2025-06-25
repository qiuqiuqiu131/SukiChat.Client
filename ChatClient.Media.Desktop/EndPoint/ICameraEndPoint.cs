using SIPSorceryMedia.Abstractions;

namespace ChatClient.Media.Desktop.EndPoint;

public interface ICameraEndPoint : IVideoSource, IVideoSink, IDisposable
{
    MediaEndPoints ToMediaEndPoints();

    bool IsRunning { get; }
}