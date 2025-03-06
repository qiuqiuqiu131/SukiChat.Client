using AutoMapper;
using ChatClient.BaseService.Mapper.Resolver;
using ChatClient.Desktop.UIEntity;
using ChatClient.Tool.Data;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Mapper;

internal class ProtoToDtoProfile : Profile
{
    public ProtoToDtoProfile()
    {
        CreateMap<UserDto, UserMessage>()
            .ForMember(um => um.RegisterTime, opt => opt.MapFrom(u => u.RegisteTime.ToString()))
            .ForMember(um => um.Birth, opt => opt.MapFrom(u => u.Birth.ToString()))
            .ForMember(um => um.IsMale, opt => opt.MapFrom(u => u.Sex == Sex.Male));

        CreateMap<UserMessage, UserDto>()
            .ForMember(u => u.RegisteTime, opt => opt.MapFrom(um => DateTime.Parse(um.RegisterTime)))
            .ForMember(u => u.Birth, opt => opt.MapFrom(um => DateOnly.Parse(um.Birth)))
            .ForMember(u => u.Sex, opt => opt.MapFrom(um => um.IsMale ? Sex.Male : Sex.Female));

        CreateMap<OutlineMessageResponse, OutlineMessageDto>()
            .ForMember(s => s.FriendRequestMessages, opt => opt.MapFrom(o => o.FriendRequests.ToList()))
            .ForMember(s => s.NewFriendMessages, opt => opt.MapFrom(o => o.NewFriends.ToList()))
            .ForMember(s => s.FriendChatMessages, opt => opt.MapFrom(o => o.FriendChats.ToList()))
            .ForMember(s => s.GroupChatMessages, opt => opt.MapFrom(o => o.GroupChats.ToList()));

        CreateMap<ChatMessage, ChatMessageDto>()
            .ForMember(s => s.Type, opt => opt.MapFrom(c => c.ContentCase))
            .ForMember(s => s.Content, opt => opt.MapFrom<ChatMessageContentResolver>());

        CreateMap<ChatMessageDto, ChatMessage>()
            .ForMember(c => c.ContentCase, opt => opt.MapFrom(s => s.Type))
            .AfterMap((src, dest, context) =>
            {
                // 根据 Type 动态映射 Content
                switch (src.Type)
                {
                    case ChatMessage.ContentOneofCase.FileMess:
                        dest.FileMess = context.Mapper.Map<FileMess>((FileMessDto)src.Content);
                        break;
                    case ChatMessage.ContentOneofCase.TextMess:
                        dest.TextMess = context.Mapper.Map<TextMess>((TextMessDto)src.Content);
                        break;
                    case ChatMessage.ContentOneofCase.ImageMess:
                        dest.ImageMess = context.Mapper.Map<ImageMess>((ImageMessDto)src.Content);
                        break;
                }
            });

        CreateMap<TextMess, TextMessDto>().ReverseMap();
        CreateMap<ImageMess, ImageMessDto>().ReverseMap();
        CreateMap<FileMess, FileMessDto>().ReverseMap();
    }
}