using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SqlSugar;

namespace ChatClient.DataBase.Data;

[Table("GroupRelations")]
[SugarTable("GroupRelations")]
[SugarIndex("unique_groupRelation_groupId_userId", nameof(GroupId), OrderByType.Asc,
    nameof(UserId), OrderByType.Asc, true)]
public class GroupRelation
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [StringLength(10)]
    [SugarColumn(Length = 10)]
    public string GroupId { get; set; }

    [StringLength(10)]
    [SugarColumn(Length = 10)]
    public string UserId { get; set; }

    public int Status { get; set; }

    [StringLength(20)] public string Grouping { get; set; }

    public DateTime JoinTime { get; set; }

    [StringLength(30)]
    [SugarColumn(IsNullable = true, Length = 30)]
    public string? NickName { get; set; }

    [StringLength(30)]
    [SugarColumn(IsNullable = true, Length = 30)]
    public string? Remark { get; set; }

    public bool CantDisturb { get; set; }

    public bool IsTop { get; set; }

    public int LastChatId { get; set; }

    public bool IsChatting { get; set; }
}