using AutoMapper;
using ChatClient.BaseService.Mapper.Resolver;
using ChatClient.DataBase.Data;
using ChatClient.Desktop.UIEntity;
using ChatClient.Tool.Data;
using ChatClient.Tool.Data.Group;
using ChatServer.Common.Protobuf;
using DryIoc.ImTools;
using Google.Protobuf.Collections;

namespace ChatClient.BaseService.Mapper;

internal class ProtoToDtoProfile : Profile
{
    public ProtoToDtoProfile()
    {
        #region UserDto + UserMessage

        CreateMap<UserDto, UserMessage>()
            .ForMember(um => um.RegisterTime, opt => opt.MapFrom(u => u.RegisteTime.ToString()))
            .ForMember(um => um.Introduction, opt => opt.MapFrom(u => u.Introduction ?? string.Empty))
            .ForMember(um => um.Birth, opt => opt.MapFrom(u => u.Birth == null ? string.Empty : u.Birth.ToString()))
            .ForMember(um => um.IsMale, opt => opt.MapFrom(u => u.Sex == Sex.Male));

        CreateMap<UserMessage, UserDto>()
            .ForMember(u => u.RegisteTime, opt => opt.MapFrom(um => DateTime.Parse(um.RegisterTime)))
            .ForMember(u => u.Introduction,
                opt => opt.MapFrom(um => string.IsNullOrEmpty(um.Introduction) ? null : um.Introduction))
            .ForMember(u => u.Birth,
                opt => opt.MapFrom(um => string.IsNullOrEmpty(um.Birth) ? (DateOnly?)null : DateOnly.Parse(um.Birth)))
            .ForMember(u => u.Sex, opt => opt.MapFrom(um => um.IsMale ? Sex.Male : Sex.Female));

        #endregion

        #region UserDetailDto + UserDetailMessage

        CreateMap<UserDetailDto, UserDetailMessage>()
            .ForMember(um => um.LastReadFriendMessageTime,
                opt => opt.MapFrom(u => u.LastReadFriendMessageTime.ToString()))
            .ForMember(um => um.LastReadGroupMessageTime,
                opt => opt.MapFrom(u => u.LastReadGroupMessageTime.ToString()))
            .ForMember(um => um.LastDeleteFriendMessageTime,
                opt => opt.MapFrom(u => u.LastDeleteFriendMessageTime.ToString()))
            .ForMember(um => um.LastDeleteGroupMessageTime,
                opt => opt.MapFrom(u => u.LastDeleteGroupMessageTime.ToString()))
            .ForMember(um => um.RegisterTime, opt => opt.MapFrom(u => u.UserDto.RegisteTime.ToString()))
            .ForMember(um => um.Introduction, opt => opt.MapFrom(u => u.UserDto.Introduction ?? string.Empty))
            .ForMember(um => um.Birth,
                opt => opt.MapFrom(u => u.UserDto.Birth == null ? string.Empty : u.UserDto.Birth.ToString()))
            .ForMember(um => um.IsMale, opt => opt.MapFrom(u => u.UserDto.Sex == Sex.Male))
            .ForMember(um => um.Name, opt => opt.MapFrom(u => u.UserDto.Name))
            .ForMember(um => um.HeadCount, opt => opt.MapFrom(u => u.UserDto.HeadCount))
            .ForMember(um => um.HeadIndex, opt => opt.MapFrom(u => u.UserDto.HeadIndex))
            .ForMember(um => um.EmailNumber, opt => opt.MapFrom(u => u.EmailNumber ?? string.Empty))
            .ForMember(um => um.PhoneNumber, opt => opt.MapFrom(u => u.PhoneNumber ?? string.Empty));

        CreateMap<UserDetailMessage, UserDetailDto>()
            .ForMember(u => u.LastReadFriendMessageTime,
                opt => opt.MapFrom(um => DateTime.Parse(um.LastReadFriendMessageTime)))
            .ForMember(u => u.LastReadGroupMessageTime,
                opt => opt.MapFrom(um => DateTime.Parse(um.LastReadGroupMessageTime)))
            .ForMember(u => u.LastDeleteFriendMessageTime,
                opt => opt.MapFrom(um => DateTime.Parse(um.LastDeleteFriendMessageTime)))
            .ForMember(u => u.LastDeleteGroupMessageTime,
                opt => opt.MapFrom(um => DateTime.Parse(um.LastDeleteGroupMessageTime)))
            .ForMember(u => u.EmailNumber,
                opt => opt.MapFrom(um => string.IsNullOrEmpty(um.EmailNumber) ? null : um.EmailNumber))
            .ForMember(u => u.PhoneNumber,
                opt => opt.MapFrom(um => string.IsNullOrEmpty(um.PhoneNumber) ? null : um.PhoneNumber));

        #endregion

        #region GroupDto + GroupMessage

        CreateMap<GroupDto, GroupMessage>()
            .ForMember(gm => gm.GroupId, opt => opt.MapFrom(g => g.Id))
            .ForMember(gm => gm.CreateTime, opt => opt.MapFrom(g => g.CreateTime.ToString()))
            .ForMember(gm => gm.Description,
                opt => opt.MapFrom(g => string.IsNullOrEmpty(g.Description) ? null : g.Description));

        CreateMap<GroupMessage, GroupDto>()
            .ForMember(g => g.Id, opt => opt.MapFrom(gm => gm.GroupId))
            .ForMember(g => g.CreateTime, opt => opt.MapFrom(gm => DateTime.Parse(gm.CreateTime)))
            .ForMember(g => g.Description, opt => opt.MapFrom(gm => gm.Description ?? string.Empty));

        #endregion

        #region GroupMemberDto + GroupMemberMessage

        CreateMap<GroupMemberMessage, GroupMemberDto>()
            .ForMember(gm => gm.JoinTime, opt =>
                opt.MapFrom(gmm =>
                    string.IsNullOrEmpty(gmm.JoinTime) ? (DateTime?)null : DateTime.Parse(gmm.JoinTime)));

        #endregion

        CreateMap<FriendRequestFromServer, FriendReceiveDto>()
            .ForMember(frd => frd.ReceiveTime, opt => opt.MapFrom(fr => DateTime.Parse(fr.RequestTime)));

        CreateMap<JoinGroupRequestFromServer, GroupReceivedDto>()
            .ForMember(jrf => jrf.UserFromId, opt => opt.MapFrom(jr => jr.UserId))
            .ForMember(jrf => jrf.ReceiveTime, opt => opt.MapFrom(jr => DateTime.Parse(jr.Time)));

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
                    case ChatMessage.ContentOneofCase.SystemMessage:
                        dest.SystemMessage = context.Mapper.Map<SystemMessage>((SystemMessDto)src.Content);
                        break;
                    case ChatMessage.ContentOneofCase.CardMess:
                        dest.CardMess = context.Mapper.Map<CardMess>((CardMessDto)src.Content);
                        break;
                    case ChatMessage.ContentOneofCase.VoiceMess:
                        dest.VoiceMess = context.Mapper.Map<VoiceMess>((VoiceMessDto)src.Content);
                        break;
                }
            });

        CreateMap<TextMess, TextMessDto>().ReverseMap();
        CreateMap<ImageMess, ImageMessDto>().ReverseMap();
        CreateMap<FileMess, FileMessDto>().ReverseMap();
        CreateMap<SystemMessage, SystemMessDto>().ReverseMap();
        CreateMap<CardMess, CardMessDto>().ReverseMap();
        CreateMap<VoiceMess, VoiceMessDto>().ReverseMap();
        CreateMap<SystemMessBlockDto, SystemMessageBlock>().ReverseMap();

        CreateMap<DeleteFriendMessage, FriendDeleteDto>()
            .ForMember(fd => fd.DeleteTime, opt => opt.MapFrom(fm => DateTime.Parse(fm.Time)))
            .ForMember(fd => fd.UseId1, opt => opt.MapFrom(fm => fm.UserId))
            .ForMember(fd => fd.UserId2, opt => opt.MapFrom(fm => fm.FriendId));

        // 添加QuitGroupMessage到GroupDelete的映射
        CreateMap<QuitGroupMessage, GroupDeleteDto>()
            .ForMember(gd => gd.DeleteId, opt => opt.MapFrom(qgm => qgm.QuitId))
            .ForMember(gd => gd.GroupId, opt => opt.MapFrom(qgm => qgm.GroupId))
            .ForMember(gd => gd.MemberId, opt => opt.MapFrom(qgm => qgm.UserId))
            .ForMember(gd => gd.OperateUserId, opt => opt.MapFrom(qgm => qgm.UserId))
            .ForMember(gd => gd.DeleteMethod, opt => opt.MapFrom(qgm => 0)) // 1表示主动退出
            .ForMember(gd => gd.DeleteTime, opt => opt.MapFrom(qgm => DateTime.Parse(qgm.Time)));

        // 添加RemoveMemberMessage到GroupDelete的映射
        CreateMap<RemoveMemberMessage, GroupDeleteDto>()
            .ForMember(gd => gd.DeleteId, opt => opt.MapFrom(rmm => rmm.RemoveId))
            .ForMember(gd => gd.GroupId, opt => opt.MapFrom(rmm => rmm.GroupId))
            .ForMember(gd => gd.MemberId, opt => opt.MapFrom(rmm => rmm.MemberId))
            .ForMember(gd => gd.OperateUserId, opt => opt.MapFrom(rmm => rmm.UserId))
            .ForMember(gd => gd.DeleteMethod, opt => opt.MapFrom(rmm => 1)) // 2表示被踢出
            .ForMember(gd => gd.DeleteTime, opt => opt.MapFrom(rmm => DateTime.Parse(rmm.Time)));

        // 添加DisbandGroupMessage到GroupDelete的映射
        CreateMap<DisbandGroupMessage, GroupDeleteDto>()
            .ForMember(gd => gd.DeleteId, opt => opt.MapFrom(dgm => dgm.DisBandId))
            .ForMember(gd => gd.GroupId, opt => opt.MapFrom(dgm => dgm.GroupId))
            .ForMember(gd => gd.DeleteMethod, opt => opt.MapFrom(dgm => 2)) // 3表示群解散
            .ForMember(gd => gd.OperateUserId, opt => opt.MapFrom(dgm => dgm.UserId))
            .ForMember(gd => gd.MemberId, opt => opt.MapFrom(dgm => dgm.MemberId))
            .ForMember(gd => gd.DeleteTime, opt => opt.MapFrom(dgm => DateTime.Parse(dgm.Time)));
    }
}