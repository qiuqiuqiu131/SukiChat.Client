using AutoMapper;
using ChatClient.DataBase.Data;
using ChatClient.Tool.Tools;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Mapper;

public class ProtoToDataProfile : Profile
{
    public ProtoToDataProfile()
    {
        CreateMap<FriendChatMessage, ChatPrivate>()
            .ForMember(cp => cp.Message, opt => opt.MapFrom(cm => ChatMessageTool.EncruptChatMessage(cm.Messages)))
            .ForMember(cp => cp.Time, opt => opt.MapFrom(cm => DateTime.Parse(cm.Time)))
            .ForMember(cp => cp.ChatId, opt => opt.MapFrom(cm => cm.Id))
            .ForMember(cp => cp.Id, opt => opt.Ignore());

        CreateMap<ChatPrivate, FriendChatMessage>()
            .ForMember(cm => cm.Messages, opt => opt.MapFrom(cp => ChatMessageTool.DecruptChatMessage(cp.Message)))
            .ForMember(cm => cm.Time, opt => opt.MapFrom(cp => cp.Time.ToString()))
            .ForMember(cm => cm.Id, opt => opt.MapFrom(cp => cp.ChatId));
    }
}