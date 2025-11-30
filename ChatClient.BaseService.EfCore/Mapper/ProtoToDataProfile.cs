using AutoMapper;
using ChatClient.DataBase.EfCore.Data;
using ChatClient.Tool.Tools;
using ChatServer.Common.Protobuf;

namespace ChatClient.BaseService.EfCore.Mapper;

public class ProtoToDataProfile : Profile
{
    public ProtoToDataProfile()
    {
        CreateMap<UserMessage, User>()
            .ForMember(u => u.RegisteTime, opt => opt.MapFrom(um => um.RegisterTime.ParseInvariant()))
            .ForMember(u => u.Introduction,
                opt => opt.MapFrom(um => string.IsNullOrEmpty(um.Introduction) ? null : um.Introduction))
            .ForMember(u => u.Birthday,
                opt => opt.MapFrom(um => string.IsNullOrEmpty(um.Birth) ? (DateTime?)null : um.Birth.ParseInvariant()));

        CreateMap<GroupMemberMessage, GroupMember>()
            .ForMember(gm => gm.JoinTime, opt =>
                opt.MapFrom(gmm =>
                    string.IsNullOrEmpty(gmm.JoinTime) ? (DateTime?)null : gmm.JoinTime.ParseInvariant()));

        CreateMap<GroupMessage, Group>()
            .ForMember(g => g.Id, opt => opt.MapFrom(gm => gm.GroupId))
            .ForMember(g => g.CreateTime, opt => opt.MapFrom(gm => gm.CreateTime.ParseInvariant()))
            .ForMember(g => g.Description, opt => opt.MapFrom(gm => gm.Description ?? string.Empty));


        #region FriendChatMessage + ChatPrivate

        CreateMap<FriendChatMessage, ChatPrivate>()
            .ForMember(cp => cp.Message, opt => opt.MapFrom(cm => ChatMessageTool.EncruptChatMessage(cm.Messages)))
            .ForMember(cp => cp.Time, opt => opt.MapFrom(cm => cm.Time.ParseInvariant()))
            .ForMember(cp => cp.ChatId, opt => opt.MapFrom(cm => cm.Id))
            .ForMember(cp => cp.Id, opt => opt.Ignore())
            .ForMember(cp => cp.RetractedTime,
                opt => opt.MapFrom(cm =>
                    string.IsNullOrWhiteSpace(cm.RetractTime) ? DateTime.MinValue : cm.RetractTime.ParseInvariant()));

        CreateMap<ChatPrivate, FriendChatMessage>()
            .ForMember(cm => cm.Messages, opt => opt.MapFrom(cp => ChatMessageTool.DecruptChatMessage(cp.Message)))
            .ForMember(cm => cm.Time, opt => opt.MapFrom(cp => cp.Time.ToInvariantString()))
            .ForMember(cm => cm.Id, opt => opt.MapFrom(cp => cp.ChatId))
            .ForMember(cm => cm.RetractTime, opt => opt.MapFrom(cp => cp.RetractedTime.ToInvariantString()));

        #endregion

        #region GroupChatMessage + ChatGroup

        CreateMap<GroupChatMessage, ChatGroup>()
            .ForMember(cg => cg.Message, opt => opt.MapFrom(gm => ChatMessageTool.EncruptChatMessage(gm.Messages)))
            .ForMember(cg => cg.Time, opt => opt.MapFrom(gm => gm.Time.ParseInvariant()))
            .ForMember(cg => cg.ChatId, opt => opt.MapFrom(gm => gm.Id))
            .ForMember(cg => cg.Id, opt => opt.Ignore())
            .ForMember(cg => cg.RetractedTime,
                opt => opt.MapFrom(gm =>
                    string.IsNullOrWhiteSpace(gm.RetractTime) ? DateTime.MinValue : gm.RetractTime.ParseInvariant()));

        CreateMap<ChatGroup, GroupChatMessage>()
            .ForMember(gm => gm.Messages, opt => opt.MapFrom(cg => ChatMessageTool.DecruptChatMessage(cg.Message)))
            .ForMember(gm => gm.Time, opt => opt.MapFrom(cg => cg.Time.ToInvariantString()))
            .ForMember(gm => gm.Id, opt => opt.MapFrom(cg => cg.ChatId))
            .ForMember(gm => gm.RetractTime, opt => opt.MapFrom(cg => cg.RetractedTime.ToInvariantString()));

        #endregion

        #region NewFriendMessage + FriendRelation

        CreateMap<NewFriendMessage, FriendRelation>()
            .ForMember(fr => fr.GroupTime, opt => opt.MapFrom(nf => nf.RelationTime.ParseInvariant()))
            .ForMember(fr => fr.User1Id, opt => opt.MapFrom(nf => nf.UserId))
            .ForMember(fr => fr.User2Id, opt => opt.MapFrom(nf => nf.FrinedId))
            .ForMember(fr => fr.Remark, opt => opt.MapFrom(nf => string.IsNullOrEmpty(nf.Remark) ? null : nf.Remark));

        CreateMap<EnterGroupMessage, GroupRelation>()
            .ForMember(gr => gr.JoinTime, opt => opt.MapFrom(egm => egm.JoinTime.ParseInvariant()))
            .ForMember(gr => gr.Remark, opt => opt.MapFrom(egm => string.IsNullOrEmpty(egm.Remark) ? null : egm.Remark))
            .ForMember(gr => gr.NickName,
                opt => opt.MapFrom(egm => string.IsNullOrEmpty(egm.NickName) ? null : egm.NickName));

        #endregion

        #region PullGroupMessage + GroupRelation

        CreateMap<PullGroupMessage, GroupRelation>()
            .ForMember(gr => gr.JoinTime, opt => opt.MapFrom(pgm => pgm.Time.ParseInvariant()))
            .ForMember(gr => gr.UserId, opt => opt.MapFrom(pgm => pgm.UserIdTarget));

        #endregion

        // User login state, get outline message and operate friend request
        CreateMap<FriendRequestMessage, FriendReceived>()
            .ForMember(fr => fr.ReceiveTime, opt => opt.MapFrom(fm => fm.RequestTime.ParseInvariant()))
            .ForMember(fr => fr.SolveTime, opt =>
                opt.MapFrom(fm =>
                    string.IsNullOrEmpty(fm.SolvedTime) ? (DateTime?)null : fm.SolvedTime.ParseInvariant()));

        CreateMap<FriendRequestMessage, FriendRequest>()
            .ForMember(fr => fr.RequestTime, opt =>
                opt.MapFrom(fm => fm.RequestTime.ParseInvariant()))
            .ForMember(fr => fr.SolveTime, opt =>
                opt.MapFrom(fm =>
                    string.IsNullOrEmpty(fm.SolvedTime) ? (DateTime?)null : fm.SolvedTime.ParseInvariant()));

        CreateMap<GroupRequestMessage, GroupRequest>()
            .ForMember(gr => gr.AcceptByUserId,
                opt => opt.MapFrom(fm => string.IsNullOrEmpty(fm.AcceptByUserId) ? null : fm.AcceptByUserId))
            .ForMember(gr => gr.RequestTime, opt => opt.MapFrom(gm => gm.RequestTime.ParseInvariant()))
            .ForMember(gr => gr.SolveTime, opt =>
                opt.MapFrom(gm =>
                    string.IsNullOrEmpty(gm.SolvedTime) ? (DateTime?)null : gm.SolvedTime.ParseInvariant()));
        CreateMap<GroupRequestMessage, GroupReceived>()
            .ForMember(gr => gr.AcceptByUserId,
                opt => opt.MapFrom(fm => string.IsNullOrEmpty(fm.AcceptByUserId) ? null : fm.AcceptByUserId))
            .ForMember(gr => gr.ReceiveTime, opt => opt.MapFrom(fm => fm.RequestTime.ParseInvariant()))
            .ForMember(gr => gr.SolveTime,
                opt => opt.MapFrom(fm =>
                    string.IsNullOrEmpty(fm.SolvedTime) ? (DateTime?)null : fm.SolvedTime.ParseInvariant()));


        CreateMap<FriendDeleteMessage, FriendDelete>()
            .ForMember(fd => fd.DeleteTime, opt => opt.MapFrom(fm => fm.Time.ParseInvariant()))
            .ForMember(fd => fd.UseId1, opt => opt.MapFrom(fm => fm.UserId))
            .ForMember(fd => fd.UserId2, opt => opt.MapFrom(fm => fm.FriendId));

        CreateMap<DeleteFriendMessage, FriendDelete>()
            .ForMember(fd => fd.DeleteTime, opt => opt.MapFrom(fm => fm.Time.ParseInvariant()))
            .ForMember(fd => fd.UseId1, opt => opt.MapFrom(fm => fm.UserId))
            .ForMember(fd => fd.UserId2, opt => opt.MapFrom(fm => fm.FriendId));


        CreateMap<GroupDeleteMessage, GroupDelete>()
            .ForMember(gd => gd.DeleteTime, opt => opt.MapFrom(gdm => gdm.Time.ParseInvariant()))
            .ForMember(gd => gd.DeleteMethod, opt => opt.MapFrom(gdm => gdm.Method))
            .ForMember(gd => gd.OperateUserId, opt => opt.MapFrom(gdm => gdm.OperateId));

        // 添加QuitGroupMessage到GroupDelete的映射
        CreateMap<QuitGroupMessage, GroupDelete>()
            .ForMember(gd => gd.DeleteId, opt => opt.MapFrom(qgm => qgm.QuitId))
            .ForMember(gd => gd.GroupId, opt => opt.MapFrom(qgm => qgm.GroupId))
            .ForMember(gd => gd.MemberId, opt => opt.MapFrom(qgm => qgm.UserId))
            .ForMember(gd => gd.OperateUserId, opt => opt.MapFrom(qgm => qgm.UserId))
            .ForMember(gd => gd.DeleteMethod, opt => opt.MapFrom(qgm => 0)) // 1表示主动退出
            .ForMember(gd => gd.DeleteTime, opt => opt.MapFrom(qgm => qgm.Time.ParseInvariant()));

        // 添加RemoveMemberMessage到GroupDelete的映射
        CreateMap<RemoveMemberMessage, GroupDelete>()
            .ForMember(gd => gd.DeleteId, opt => opt.MapFrom(rmm => rmm.RemoveId))
            .ForMember(gd => gd.GroupId, opt => opt.MapFrom(rmm => rmm.GroupId))
            .ForMember(gd => gd.MemberId, opt => opt.MapFrom(rmm => rmm.MemberId))
            .ForMember(gd => gd.OperateUserId, opt => opt.MapFrom(rmm => rmm.UserId))
            .ForMember(gd => gd.DeleteMethod, opt => opt.MapFrom(rmm => 1)) // 2表示被踢出
            .ForMember(gd => gd.DeleteTime, opt => opt.MapFrom(rmm => rmm.Time.ParseInvariant()));

        // 添加DisbandGroupMessage到GroupDelete的映射
        CreateMap<DisbandGroupMessage, GroupDelete>()
            .ForMember(gd => gd.DeleteId, opt => opt.MapFrom(dgm => dgm.DisBandId))
            .ForMember(gd => gd.GroupId, opt => opt.MapFrom(dgm => dgm.GroupId))
            .ForMember(gd => gd.DeleteMethod, opt => opt.MapFrom(dgm => 2)) // 3表示群解散
            .ForMember(gd => gd.OperateUserId, opt => opt.MapFrom(dgm => dgm.UserId))
            .ForMember(gd => gd.MemberId, opt => opt.MapFrom(dgm => dgm.MemberId))
            .ForMember(gd => gd.DeleteTime, opt => opt.MapFrom(dgm => dgm.Time.ParseInvariant()));


        CreateMap<ChatPrivateDetailMessage, ChatPrivateDetail>();
        CreateMap<ChatGroupDetailMessage, ChatGroupDetail>();
    }
}