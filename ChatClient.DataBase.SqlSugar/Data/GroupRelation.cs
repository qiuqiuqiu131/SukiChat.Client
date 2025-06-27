using SqlSugar;

namespace ChatClient.DataBase.SqlSugar.Data;

[SugarTable("GroupRelations")]
[SugarIndex("unique_groupRelation_groupId_userId", nameof(GroupId), OrderByType.Asc,
    nameof(UserId), OrderByType.Asc, true)]
public class GroupRelation
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }
    
    [SugarColumn(Length = 10)]
    public string GroupId { get; set; }
    
    [SugarColumn(Length = 10)]
    public string UserId { get; set; }

    public int Status { get; set; }

    public string Grouping { get; set; }

    public DateTime JoinTime { get; set; }
    
    [SugarColumn(IsNullable = true, Length = 30)]
    public string? NickName { get; set; }
    
    [SugarColumn(IsNullable = true, Length = 30)]
    public string? Remark { get; set; }

    public bool CantDisturb { get; set; }

    public bool IsTop { get; set; }

    public int LastChatId { get; set; }

    public bool IsChatting { get; set; }
}