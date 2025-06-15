namespace ChatClient.Tool.Media.Audio;

public enum RecordingState
{
    Recording,
    Paused,
    Stopped
}

public class RecordingStateChangedEventArgs : EventArgs
{
    public RecordingState State { get; }

    public RecordingStateChangedEventArgs(RecordingState state)
    {
        State = state;
    }
}

public class AudioLevelEventArgs : EventArgs
{
    public float Level { get; }

    public AudioLevelEventArgs(float level)
    {
        Level = level;
    }
}

public class AudioRecordingException : Exception
{
    public AudioRecordingException(string message) : base(message)
    {
    }

    public AudioRecordingException(string message, Exception innerException) : base(message, innerException)
    {
    }
}