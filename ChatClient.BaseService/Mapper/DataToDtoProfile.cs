using AutoMapper;
using ChatClient.DataBase.Data;
using ChatClient.Desktop.UIEntity;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Group;
using ChatClient.Tool.Tools;
using ChatServer.Common.Protobuf;
using DryIoc.ImTools;

namespace ChatClient.BaseService.Mapper;

internal class DataToDtoProfile : Profile
{
    public DataToDtoProfile()
    {
        #region UserDto + User

        CreateMap<UserDto, User>()
            .ForMember(u => u.isMale, opt => opt.MapFrom(ud => ud.Sex == Sex.Male ? true : false));
        CreateMap<User, UserDto>()
            .ForMember(ud => ud.Sex, opt => opt.MapFrom(u => u.isMale ? Sex.Male : Sex.Female));

        #endregion

        CreateMap<GroupDto, Group>().ReverseMap();

        CreateMap<GroupMemberDto, GroupMember>().ReverseMap();

        CreateMap<FriendReceived, FriendReceiveDto>().ReverseMap();
        CreateMap<FriendRequest, FriendRequestDto>().ReverseMap();

        CreateMap<FriendRelation, FriendRelationDto>()
            .ForMember(fr => fr.Id, opt => opt.MapFrom(frd => frd.User2Id));

        CreateMap<GroupRelation, GroupRelationDto>()
            .ForMember(gr => gr.Id, opt => opt.MapFrom(grd => grd.GroupId));

        // User login state, get outline message and operate friend request
        CreateMap<FriendRequestMessage, FriendRequest>()
            .ForMember(fr => fr.RequestTime, opt => opt.MapFrom(fm => DateTime.Parse(fm.RequestTime)))
            .ForMember(fr => fr.SolveTime, opt => opt.MapFrom(fm => DateTime.Parse(fm.SolvedTime)));
        CreateMap<FriendRequestMessage, FriendReceived>()
            .ForMember(fr => fr.ReceiveTime, opt => opt.MapFrom(fm => DateTime.Parse(fm.RequestTime)))
            .ForMember(fr => fr.SolveTime, opt => opt.MapFrom(fm => DateTime.Parse(fm.SolvedTime)));

        CreateMap<ChatPrivate, ChatData>()
            .ForMember(cd => cd.ChatMessages,
                opt => opt.MapFrom(d => ChatMessageTool.DecruptChatMessageDto(d.Message)));

        CreateMap<ChatGroup, GroupChatData>()
            .ForMember(gcd => gcd.ChatMessages,
                opt => opt.MapFrom(cg => ChatMessageTool.DecruptChatMessageDto(cg.Message)));
    }
}