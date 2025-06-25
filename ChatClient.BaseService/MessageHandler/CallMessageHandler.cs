using ChatClient.Tool.Events;
using ChatClient.Tool.Media.Call;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.MessageHandler;

public class CallMessageHandler : MessageHandlerBase
{
    private readonly IContainerProvider _containerProvider;
    private readonly ICallOperator _callOperator;
    private readonly ICallManager _callManager;

    public CallMessageHandler(IContainerProvider containerProvider, ICallOperator callOperator) : base(
        containerProvider)
    {
        _containerProvider = containerProvider;
        _callOperator = callOperator;
    }

    protected override void OnRegisterEvent(IEventAggregator eventAggregator)
    {
        var token1 = eventAggregator.GetEvent<ResponseEvent<SignalingMessage>>()
            .Subscribe(OnSignalingMessage);
        _subscriptionTokens.Add(token1);

        var token2 = eventAggregator.GetEvent<ResponseEvent<HangUp>>()
            .Subscribe(OnHangUp);
        _subscriptionTokens.Add(token2);
    }

    private void OnHangUp(HangUp obj)
    {
        _callOperator.HandleHangup(obj.From);
    }

    private void OnSignalingMessage(SignalingMessage obj)
    {
        if (obj.Type == SignalingType.Answer)
            _callOperator.HandleAnswer(obj.From, obj.Sdp);
        else if (obj.Type == SignalingType.Offer)
            _callOperator.HandleOffer(obj.From, obj.Sdp);
        else if (obj.Type == SignalingType.IceCandidate)
            _callOperator.HandleIceCandidate(obj.From, obj.Ice);
    }
}