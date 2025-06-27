using SqlSugar;

namespace ChatClient.DataBase.SqlSugar.Data;

[SugarTable("GroupMembers")]
[SugarIndex("unique_groupMember_groupId_userId", nameof(GroupId), OrderByType.Asc,
    nameof(UserId), OrderByType.Asc, true)]
public class GroupMember
{
    [SugarColumn(IsPrimaryKey = true, Length = 10)]
    public string GroupId { get; set; }
    
    [SugarColumn(IsPrimaryKey = true, Length = 10)]
    public string UserId { get; set; }

    public int Status { get; set; }

    public DateTime JoinTime { get; set; }
    
    [SugarColumn(IsNullable = true, Length = 30)]
    public string? NickName { get; set; }

    [SugarColumn(DefaultValue = "-1")] public int HeadIndex { get; set; } = -1;
}