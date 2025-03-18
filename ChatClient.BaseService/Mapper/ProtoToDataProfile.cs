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

        #region PullGroupMessage + GroupRelation

        CreateMap<PullGroupMessage, GroupRelation>()
            .ForMember(gr => gr.JoinTime, opt => opt.MapFrom(pgm => DateTime.Parse(pgm.Time)))
            .ForMember(gr => gr.UserId, opt => opt.MapFrom(pgm => pgm.UserIdTarget));

        #endregion

        // User login state, get outline message and operate friend request
        CreateMap<FriendRequestMessage, FriendReceived>()
            .ForMember(fr => fr.ReceiveTime, opt => opt.MapFrom(fm => DateTime.Parse(fm.RequestTime)))
            .ForMember(fr => fr.SolveTime, opt => opt.MapFrom(fm => DateTime.Parse(fm.SolvedTime)));

        CreateMap<FriendRequestMessage, FriendRequest>()
            .ForMember(fr => fr.RequestTime, opt =>
                opt.MapFrom(
                    fm => DateTime.Parse(fm.RequestTime)))
            .ForMember(fr => fr.SolveTime, opt => opt.MapFrom(fm => DateTime.Parse(fm.SolvedTime)));

        CreateMap<GroupRequestMessage, GroupRequest>()
            .ForMember(gr => gr.AcceptByUserId,
                opt => opt.MapFrom(fm => string.IsNullOrEmpty(fm.AcceptByUserId) ? null : fm.AcceptByUserId))
            .ForMember(gr => gr.RequestTime, opt => opt.MapFrom(gm => DateTime.Parse(gm.RequestTime)))
            .ForMember(gr => gr.SolveTime, opt =>
                opt.MapFrom(gm =>
                    string.IsNullOrEmpty(gm.SolvedTime) ? (DateTime?)null : DateTime.Parse(gm.SolvedTime)));
        CreateMap<GroupRequestMessage, GroupReceived>()
            .ForMember(gr => gr.AcceptByUserId,
                opt => opt.MapFrom(fm => string.IsNullOrEmpty(fm.AcceptByUserId) ? null : fm.AcceptByUserId))
            .ForMember(gr => gr.ReceiveTime, opt => opt.MapFrom(fm => DateTime.Parse(fm.RequestTime)))
            .ForMember(gr => gr.SolveTime,
                opt => opt.MapFrom(fm =>
                    string.IsNullOrEmpty(fm.SolvedTime) ? (DateTime?)null : DateTime.Parse(fm.SolvedTime)));


        CreateMap<FriendDeleteMessage, FriendDelete>()
            .ForMember(fd => fd.DeleteTime, opt => opt.MapFrom(fm => DateTime.Parse(fm.Time)))
            .ForMember(fd => fd.UseId1, opt => opt.MapFrom(fm => fm.UserId))
            .ForMember(fd => fd.UserId2, opt => opt.MapFrom(fm => fm.FriendId));

        CreateMap<DeleteFriendMessage, FriendDelete>()
            .ForMember(fd => fd.DeleteTime, opt => opt.MapFrom(fm => DateTime.Parse(fm.Time)))
            .ForMember(fd => fd.UseId1, opt => opt.MapFrom(fm => fm.UserId))
            .ForMember(fd => fd.UserId2, opt => opt.MapFrom(fm => fm.FriendId));


        CreateMap<GroupDeleteMessage, GroupDelete>()
            .ForMember(gd => gd.DeleteTime, opt => opt.MapFrom(gdm => DateTime.Parse(gdm.Time)))
            .ForMember(gd => gd.DeleteMethod, opt => opt.MapFrom(gdm => gdm.Method));

        // 添加QuitGroupMessage到GroupDelete的映射
        CreateMap<QuitGroupMessage, GroupDelete>()
            .ForMember(gd => gd.DeleteId, opt => opt.MapFrom(qgm => qgm.QuitId))
            .ForMember(gd => gd.GroupId, opt => opt.MapFrom(qgm => qgm.GroupId))
            .ForMember(gd => gd.MemberId, opt => opt.MapFrom(qgm => qgm.UserId))
            .ForMember(gd => gd.OperateUserId, opt => opt.MapFrom(qgm => qgm.UserId))
            .ForMember(gd => gd.DeleteMethod, opt => opt.MapFrom(qgm => 1)) // 1表示主动退出
            .ForMember(gd => gd.DeleteTime, opt => opt.MapFrom(qgm => DateTime.Parse(qgm.Time)));

        // 添加RemoveMemberMessage到GroupDelete的映射
        CreateMap<RemoveMemberMessage, GroupDelete>()
            .ForMember(gd => gd.DeleteId, opt => opt.MapFrom(rmm => rmm.RemoveId))
            .ForMember(gd => gd.GroupId, opt => opt.MapFrom(rmm => rmm.GroupId))
            .ForMember(gd => gd.MemberId, opt => opt.MapFrom(rmm => rmm.MemberId))
            .ForMember(gd => gd.OperateUserId, opt => opt.MapFrom(rmm => rmm.UserId))
            .ForMember(gd => gd.DeleteMethod, opt => opt.MapFrom(rmm => 2)) // 2表示被踢出
            .ForMember(gd => gd.DeleteTime, opt => opt.MapFrom(rmm => DateTime.Parse(rmm.Time)));

        // 添加DisbandGroupMessage到GroupDelete的映射
        CreateMap<DisbandGroupMessage, GroupDelete>()
            .ForMember(gd => gd.DeleteId, opt => opt.MapFrom(dgm => dgm.DisBandId))
            .ForMember(gd => gd.GroupId, opt => opt.MapFrom(dgm => dgm.GroupId))
            .ForMember(gd => gd.DeleteMethod, opt => opt.MapFrom(dgm => 3)) // 3表示群解散
            .ForMember(gd => gd.OperateUserId, opt => opt.MapFrom(dgm => dgm.UserId))
            .ForMember(gd => gd.MemberId, opt => opt.MapFrom(dgm => dgm.MemberId))
            .ForMember(gd => gd.DeleteTime, opt => opt.MapFrom(dgm => DateTime.Parse(dgm.Time)));
    }
}