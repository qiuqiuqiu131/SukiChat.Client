using ChatServer.Common.Protobuf;

namespace ChatClient.Media.CallOperator;

public interface ICallOperator
{
    /// <summary>
    /// 处理收到的Offer
    /// </summary>
    Task HandleOffer(string from, string sdp);

    /// <summary>
    /// 处理收到的Answer
    /// </summary>
    Task HandleAnswer(string from, string sdp);

    /// <summary>
    /// 处理ICE候选
    /// </summary>
    Task HandleIceCandidate(string from, IceCandidate iceCandidate);

    /// <summary>
    /// 处理挂断
    /// </summary>
    Task<bool> HandleHangup(string from);
}