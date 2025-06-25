using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SqlSugar;

namespace ChatClient.DataBase.Data;

[Table("GroupMembers")]
[SugarTable("GroupMembers")]
[PrimaryKey(nameof(GroupId), nameof(UserId))]
[SugarIndex("unique_groupMember_groupId_userId", nameof(GroupId), OrderByType.Asc,
    nameof(UserId), OrderByType.Asc, true)]
public class GroupMember
{
    [StringLength(10)]
    [SugarColumn(IsPrimaryKey = true, Length = 10)]
    public string GroupId { get; set; }

    [StringLength(10)]
    [SugarColumn(IsPrimaryKey = true, Length = 10)]
    public string UserId { get; set; }

    public int Status { get; set; }

    public DateTime JoinTime { get; set; }

    [StringLength(30)]
    [SugarColumn(IsNullable = true, Length = 30)]
    public string? NickName { get; set; }

    [SugarColumn(DefaultValue = "-1")] public int HeadIndex { get; set; } = -1;
}