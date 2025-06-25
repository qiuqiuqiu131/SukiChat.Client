using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SqlSugar;

namespace ChatClient.DataBase.Data;

[Table("FriendRelations")]
[SugarTable("FriendRelations")]
[PrimaryKey(nameof(User1Id), nameof(User2Id))]
[SugarIndex("unique_friendRelation_user1Id_user2Id", nameof(User1Id), OrderByType.Asc,
    nameof(User2Id), OrderByType.Asc, true)]
public class FriendRelation
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [StringLength(10)]
    [SugarColumn(Length = 10)]
    public string User1Id { get; set; }

    [StringLength(10)]
    [SugarColumn(Length = 10)]
    public string User2Id { get; set; }

    [Required]
    [StringLength(20)]
    [SugarColumn(Length = 20)]
    public string Grouping { get; set; }

    [Required] public DateTime GroupTime { get; set; }

    [SugarColumn(IsNullable = true)] public string? Remark { get; set; }

    public bool CantDisturb { get; set; }

    public bool IsTop { get; set; }

    public int LastChatId { get; set; }

    [SugarColumn(DefaultValue = "1")] public bool IsChatting { get; set; } = true;
}