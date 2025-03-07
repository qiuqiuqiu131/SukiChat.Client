using AutoMapper;
using ChatClient.DataBase.Data;
using ChatClient.Tool.Tools;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.Mapper;

public class ProtoToDataProfile : Profile
{
    public ProtoToDataProfile()
    {
        #region FriendChatMessage + ChatPrivate

        CreateMap<FriendChatMessage, ChatPrivate>()
            .ForMember(cp => cp.Message, opt => opt.MapFrom(cm => ChatMessageTool.EncruptChatMessage(cm.Messages)))
            .ForMember(cp => cp.Time, opt => opt.MapFrom(cm => DateTime.Parse(cm.Time)))
            .ForMember(cp => cp.ChatId, opt => opt.MapFrom(cm => cm.Id))
            .ForMember(cp => cp.Id, opt => opt.Ignore());

        CreateMap<ChatPrivate, FriendChatMessage>()
            .ForMember(cm => cm.Messages, opt => opt.MapFrom(cp => ChatMessageTool.DecruptChatMessage(cp.Message)))
            .ForMember(cm => cm.Time, opt => opt.MapFrom(cp => cp.Time.ToString()))
            .ForMember(cm => cm.Id, opt => opt.MapFrom(cp => cp.ChatId));

        #endregion

        #region GroupChatMessage + ChatGroup

        CreateMap<GroupChatMessage, ChatGroup>()
            .ForMember(cg => cg.Message, opt => opt.MapFrom(gm => ChatMessageTool.EncruptChatMessage(gm.Messages)))
            .ForMember(cg => cg.Time, opt => opt.MapFrom(gm => DateTime.Parse(gm.Time)))
            .ForMember(cg => cg.ChatId, opt => opt.MapFrom(gm => gm.Id))
            .ForMember(cg => cg.Id, opt => opt.Ignore());

        CreateMap<ChatGroup, GroupChatMessage>()
            .ForMember(gm => gm.Messages, opt => opt.MapFrom(cg => ChatMessageTool.DecruptChatMessage(cg.Message)))
            .ForMember(gm => gm.Time, opt => opt.MapFrom(cg => cg.Time.ToString()))
            .ForMember(gm => gm.Id, opt => opt.MapFrom(cg => cg.ChatId));

        #endregion

        #region NewFriendMessage + FriendRelation

        CreateMap<NewFriendMessage, FriendRelation>()
            .ForMember(fr => fr.GroupTime, opt => opt.MapFrom(nf => DateTime.Parse(nf.RelationTime)))
            .ForMember(fr => fr.User1Id, opt => opt.MapFrom(nf => nf.UserId))
            .ForMember(fr => fr.User2Id, opt => opt.MapFrom(nf => nf.FrinedId))
            .ForMember(fr => fr.Remark, opt => opt.MapFrom(nf => string.IsNullOrEmpty(nf.Remark) ? null : nf.Remark));

        CreateMap<EnterGroupMessage, GroupRelation>()
            .ForMember(gr => gr.JoinTime, opt => opt.MapFrom(egm => DateTime.Parse(egm.JoinTime)))
            .ForMember(gr => gr.Remark, opt => opt.MapFrom(egm => string.IsNullOrEmpty(egm.Remark) ? null : egm.Remark))
            .ForMember(gr => gr.NickName,
                opt => opt.MapFrom(egm => string.IsNullOrEmpty(egm.NickName) ? null : egm.NickName));

        #endregion
    }
}