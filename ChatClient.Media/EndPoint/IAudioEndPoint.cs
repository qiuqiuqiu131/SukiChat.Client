using SIPSorceryMedia.Abstractions;

namespace ChatClient.Media.EndPoint;

public interface IAudioEndPoint : IAudioSource, IAudioSink, IDisposable
{
    MediaEndPoints ToMediaEndPoints();
}