using SqlSugar;

namespace ChatClient.DataBase.SqlSugar.Data;

[SugarTable("FriendRelations")]
[SugarIndex("unique_friendRelation_user1Id_user2Id", nameof(User1Id), OrderByType.Asc,
    nameof(User2Id), OrderByType.Asc, true)]
public class FriendRelation
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [SugarColumn(Length = 10)] public string User1Id { get; set; }

    [SugarColumn(Length = 10)] public string User2Id { get; set; }

    [SugarColumn(Length = 20)] public string Grouping { get; set; }

    public DateTime GroupTime { get; set; }

    [SugarColumn(IsNullable = true)] public string? Remark { get; set; }

    public bool CantDisturb { get; set; }

    public bool IsTop { get; set; }

    public int LastChatId { get; set; }

    [SugarColumn(DefaultValue = "1")] public bool IsChatting { get; set; } = true;
}