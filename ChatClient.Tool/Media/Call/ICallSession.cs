namespace ChatClient.Tool.Media.Call;

public interface ICallSession
{
    /// <summary>
    /// 发起通话请求
    /// </summary>
    Task<bool> InitiateCall(string peerId, CancellationTokenSource? cancellationToken);

    /// <summary>
    /// 接听呼叫
    /// </summary>
    Task<bool> AcceptCall(string callerId);

    /// <summary>
    /// 拒绝呼叫
    /// </summary>
    Task RejectCall(string callerId);

    /// <summary>
    /// 结束通话
    /// </summary>
    Task EndCall();
}