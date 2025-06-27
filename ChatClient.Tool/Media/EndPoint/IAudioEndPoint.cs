using SIPSorceryMedia.Abstractions;

namespace ChatClient.Tool.Media.EndPoint;

public interface IAudioEndPoint : IAudioSource, IAudioSink, IDisposable
{
    MediaEndPoints ToMediaEndPoints();
}