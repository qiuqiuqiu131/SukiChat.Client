using SIPSorceryMedia.Abstractions;

namespace ChatClient.Media.Desktop.EndPoint;

public interface IAudioEndPoint : IAudioSource, IAudioSink, IDisposable
{
    MediaEndPoints ToMediaEndPoints();
}