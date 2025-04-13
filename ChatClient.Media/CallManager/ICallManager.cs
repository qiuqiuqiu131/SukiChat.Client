using ChatClient.Media.CallOperator;
using ChatServer.Common.Protobuf;

namespace ChatClient.Media.CallManager;

public interface ICallManager
{
    Task<CallOperatorBase?> StartCall(CallType callType);
    Task<CallOperatorBase?> GetCallRequest(CallRequest callRequest);
    Task RemoveCall();
}