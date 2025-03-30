using ChatClient.DataBase.Data;
using ChatClient.Tool.ManagerInterface;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.DataBase;

public class ChatClientDbContext : DbContext
{
    public DbSet<LoginHistory> LoginHistory { get; set; }
    public DbSet<User> User { get; set; }
    public DbSet<ChatPrivate> ChatPrivate { get; set; }
    public DbSet<ChatPrivateDetail> ChatPrivateDetails { get; set; }
    public DbSet<ChatPrivateFile> ChatPrivateFiles { get; set; }
    public DbSet<FriendRelation> FriendRelation { get; set; }
    public DbSet<FriendRequest> FriendRequest { get; set; }
    public DbSet<FriendReceived> FriendReceived { get; set; }
    public DbSet<FriendDelete> FriendDelete { get; set; }

    public DbSet<Group> Group { get; set; }
    public DbSet<GroupRequest> GroupRequest { get; set; }
    public DbSet<GroupReceived> GroupReceiveds { get; set; }
    public DbSet<GroupRelation> GroupRelation { get; set; }
    public DbSet<ChatGroup> ChatGroup { get; set; }
    public DbSet<ChatGroupDetail> ChatGroupDetails { get; set; }
    public DbSet<ChatGroupFile> ChatGroupFiles { get; set; }
    public DbSet<GroupMember> GroupMember { get; set; }
    public DbSet<GroupDelete> GroupDelete { get; set; }

    private readonly string _databasePath;

    public ChatClientDbContext(IAppDataManager appDataManager) : base()
    {
        _databasePath = appDataManager.GetFileInfo("ChatClient.db").FullName;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={_databasePath}");
    }
}