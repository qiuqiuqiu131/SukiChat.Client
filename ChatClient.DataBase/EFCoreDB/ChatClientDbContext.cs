using ChatClient.DataBase.Data;
using ChatClient.Tool.ManagerInterface;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.DataBase.EFCoreDB;

public class ChatClientDbContext : DbContext
{
    public DbSet<LoginHistory> LoginHistorys { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<ChatPrivate> ChatPrivates { get; set; }
    public DbSet<ChatPrivateDetail> ChatPrivateDetails { get; set; }
    public DbSet<ChatPrivateFile> ChatPrivateFiles { get; set; }
    public DbSet<FriendRelation> FriendRelations { get; set; }
    public DbSet<FriendRequest> FriendRequests { get; set; }
    public DbSet<FriendReceived> FriendReceiveds { get; set; }
    public DbSet<FriendDelete> FriendDeletes { get; set; }

    public DbSet<Group> Groups { get; set; }
    public DbSet<GroupRequest> GroupRequests { get; set; }
    public DbSet<GroupReceived> GroupReceiveds { get; set; }
    public DbSet<GroupRelation> GroupRelations { get; set; }
    public DbSet<ChatGroup> ChatGroups { get; set; }
    public DbSet<ChatGroupDetail> ChatGroupDetails { get; set; }
    public DbSet<ChatGroupFile> ChatGroupFiles { get; set; }
    public DbSet<GroupMember> GroupMembers { get; set; }
    public DbSet<GroupDelete> GroupDeletes { get; set; }

    private readonly string _databasePath;

    public ChatClientDbContext(IAppDataManager appDataManager) : base()
    {
        _databasePath = appDataManager.GetFileInfo("ChatClient.db").FullName;
    }

    //public ChatClientDbContext()
    //{

    //}

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={_databasePath}");
        // optionsBuilder.UseSqlite($"Data Source=ChatClient.db");

        // 如果是AOT环境就使用预编译模型
        // if (!RuntimeFeature.IsDynamicCodeSupported)
        // {
        //     optionsBuilder.UseModel(ChatClientDbContextModel.Instance);
        // }
    }
}