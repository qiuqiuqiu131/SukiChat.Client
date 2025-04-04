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

        CreateMap<User, UserDto>()
            .ForMember(ud => ud.Sex, opt => opt.MapFrom(u => u.isMale ? Sex.Male : Sex.Female));

        #endregion

        #region UserDetailDto + User

        CreateMap<UserDetailDto, User>()
            .ForMember(um => um.RegisteTime, opt => opt.MapFrom(u => u.UserDto.RegisteTime))
            .ForMember(um => um.Introduction, opt => opt.MapFrom(u => u.UserDto.Introduction ?? string.Empty))
            .ForMember(um => um.Birthday,
                opt => opt.MapFrom(u => u.UserDto.Birth))
            .ForMember(um => um.isMale, opt => opt.MapFrom(u => u.UserDto.Sex == Sex.Male))
            .ForMember(um => um.Name, opt => opt.MapFrom(u => u.UserDto.Name))
            .ForMember(um => um.HeadCount, opt => opt.MapFrom(u => u.UserDto.HeadCount))
            .ForMember(um => um.HeadIndex, opt => opt.MapFrom(u => u.UserDto.HeadIndex));

        #endregion

        CreateMap<GroupDto, Group>().ReverseMap();

        CreateMap<GroupMemberDto, GroupMember>().ReverseMap();

        CreateMap<FriendReceived, FriendReceiveDto>().ReverseMap();
        CreateMap<FriendRequest, FriendRequestDto>().ReverseMap();

        CreateMap<GroupRequestDto, GroupRequest>().ReverseMap();
        CreateMap<GroupReceivedDto, GroupReceived>().ReverseMap();

        CreateMap<FriendRelation, FriendRelationDto>()
            .ForMember(fr => fr.Id, opt => opt.MapFrom(frd => frd.User2Id));

        CreateMap<GroupRelation, GroupRelationDto>()
            .ForMember(gr => gr.Id, opt => opt.MapFrom(grd => grd.GroupId));

        CreateMap<ChatPrivate, ChatData>()
            .ForMember(cd => cd.ChatMessages,
                opt => opt.MapFrom(d => ChatMessageTool.DecruptChatMessageDto(d.Message)));

        CreateMap<ChatGroup, GroupChatData>()
            .ForMember(gcd => gcd.ChatMessages,
                opt => opt.MapFrom(cg => ChatMessageTool.DecruptChatMessageDto(cg.Message)));

        CreateMap<FriendDelete, FriendDeleteDto>().ReverseMap();
        CreateMap<GroupDelete, GroupDeleteDto>().ReverseMap();
    }
}