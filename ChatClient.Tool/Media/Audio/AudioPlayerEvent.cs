using NAudio.Wave;

namespace ChatClient.Tool.Media.Audio;

public class PlaybackStateChangedEventArgs : EventArgs
{
    public PlaybackState State { get; }

    public PlaybackStateChangedEventArgs(PlaybackState state)
    {
        State = state;
    }
}

public class PlaybackPositionEventArgs : EventArgs
{
    public TimeSpan CurrentPosition { get; }
    public TimeSpan TotalTime { get; }

    public double ProgressPercentage => TotalTime.TotalSeconds > 0
        ? CurrentPosition.TotalSeconds / TotalTime.TotalSeconds
        : 0;

    public PlaybackPositionEventArgs(TimeSpan currentPosition, TimeSpan totalTime)
    {
        CurrentPosition = currentPosition;
        TotalTime = totalTime;
    }
}

public class AudioPlayerException : Exception
{
    public AudioPlayerException(string message) : base(message)
    {
    }

    public AudioPlayerException(string message, Exception innerException) : base(message, innerException)
    {
    }
}