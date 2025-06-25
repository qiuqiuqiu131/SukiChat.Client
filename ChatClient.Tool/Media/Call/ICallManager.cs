using ChatServer.Common.Protobuf;

namespace ChatClient.Tool.Media.Call;

public interface ICallManager
{
    Task<CallOperatorBase?> StartCall(CallType callType);
    Task<CallOperatorBase?> GetCallRequest(CallRequest callRequest);
    Task RemoveCall();
}