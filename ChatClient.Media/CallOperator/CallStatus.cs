namespace ChatClient.Media.CallOperator;

public enum CallStatus
{
    Idle, // 空闲状态
    Calling, // 正在呼叫
    InCall, // 通话中
    Ended, // 通话结束
    Rejected, // 被拒绝
    Error // 错误
}