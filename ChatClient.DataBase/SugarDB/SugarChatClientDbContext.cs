using ChatClient.DataBase.Data;
using ChatClient.DataBase.SugarDB.UnitOfWork;

namespace ChatClient.DataBase.SugarDB;

public class SugarChatClientDbContext : SugarDbContext
{
    public SugarDbSet<LoginHistory> LoginHistory { get; set; }
    public SugarDbSet<User> User { get; set; }
    public SugarDbSet<ChatPrivate> ChatPrivate { get; set; }
    public SugarDbSet<ChatPrivateDetail> ChatPrivateDetails { get; set; }
    public SugarDbSet<ChatPrivateFile> ChatPrivateFiles { get; set; }
    public SugarDbSet<FriendRelation> FriendRelation { get; set; }
    public SugarDbSet<FriendRequest> FriendRequest { get; set; }
    public SugarDbSet<FriendReceived> FriendReceived { get; set; }
    public SugarDbSet<FriendDelete> FriendDelete { get; set; }

    public SugarDbSet<Group> Group { get; set; }
    public SugarDbSet<GroupRequest> GroupRequest { get; set; }
    public SugarDbSet<GroupReceived> GroupReceiveds { get; set; }
    public SugarDbSet<GroupRelation> GroupRelation { get; set; }
    public SugarDbSet<ChatGroup> ChatGroup { get; set; }
    public SugarDbSet<ChatGroupDetail> ChatGroupDetails { get; set; }
    public SugarDbSet<ChatGroupFile> ChatGroupFiles { get; set; }
    public SugarDbSet<GroupMember> GroupMember { get; set; }
    public SugarDbSet<GroupDelete> GroupDelete { get; set; }
}