namespace ChatClient.Media.CallOperator;

public class CallException : Exception
{
    public CallExceptionType Type { get; set; }

    public CallException(CallExceptionType type, string message) : base(message)
    {
        Type = type;
    }

    public CallException(CallExceptionType type)
    {
        Type = type;
    }
}

public enum CallExceptionType
{
    // 请求通话超时
    RequestTimeout,

    // 已经在通话
    AlreadyInCall,

    OutLine
}