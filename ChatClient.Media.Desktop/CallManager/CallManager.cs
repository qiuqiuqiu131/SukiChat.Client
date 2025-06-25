using ChatClient.Media.Desktop.CallOperator;
using ChatClient.Tool.HelperInterface;
using ChatClient.Tool.ManagerInterface;
using ChatClient.Tool.Media.Call;
using ChatServer.Common.Protobuf;

namespace ChatClient.Media.Desktop.CallManager;

public class CallManager : ICallManager, ICallOperator
{
    private readonly IUserManager _userManager;
    private readonly IContainerProvider _containerProvider;

    private CallOperatorBase? CallOperator;

    public CallManager(IUserManager userManager, IContainerProvider containerProvider)
    {
        _userManager = userManager;
        _containerProvider = containerProvider;
    }

    public async Task<CallOperatorBase?> StartCall(CallType callType)
    {
        if (_userManager.User == null) return null;
        if (CallOperator != null)
            throw new CallException(CallExceptionType.AlreadyInCall);

        if (callType == CallType.Telephone)
            CallOperator = _containerProvider.Resolve<TelephoneCallOperator>();
        else if (callType == CallType.Video)
            CallOperator = _containerProvider.Resolve<VideoCallOperator>();

        return CallOperator;
    }

    public async Task<CallOperatorBase?> GetCallRequest(CallRequest callRequest)
    {
        if (CallOperator != null)
        {
            var messageHelper = _containerProvider.Resolve<IMessageHelper>();
            var request = new CallResponse
            {
                Accept = false,
                Callee = _userManager.User.Id,
                Caller = callRequest.Caller,
                Response = new CommonResponse { State = true }
            };
            await messageHelper.SendMessage(request);
            return null;
        }

        if (callRequest.Type == CallType.Telephone)
            CallOperator = _containerProvider.Resolve<TelephoneCallOperator>();
        else if (callRequest.Type == CallType.Video)
            CallOperator = _containerProvider.Resolve<VideoCallOperator>();

        return CallOperator;
    }

    public async Task RemoveCall()
    {
        // CallOperator?.EndCall();
        CallOperator = null;
    }

    public Task HandleOffer(string from, string sdp)
    {
        if (CallOperator != null)
            return CallOperator.HandleOffer(from, sdp);
        return Task.CompletedTask;
    }

    public Task HandleAnswer(string from, string sdp)
    {
        if (CallOperator != null)
            return CallOperator.HandleAnswer(from, sdp);
        return Task.CompletedTask;
    }

    public Task HandleIceCandidate(string from, IceCandidate iceCandidate)
    {
        if (CallOperator != null)
            return CallOperator.HandleIceCandidate(from, iceCandidate);
        return Task.CompletedTask;
    }

    public async Task<bool> HandleHangup(string from)
    {
        if (CallOperator != null)
            await CallOperator.HandleHangup(from);
        CallOperator = null;
        return true;
    }
}