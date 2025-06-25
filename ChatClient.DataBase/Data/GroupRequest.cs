using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SqlSugar;

namespace ChatClient.DataBase.Data;

[Table("GroupRequests")]
[SugarTable("GroupRequests")]
[SugarIndex("unique_groupRequest_requestId", nameof(RequestId), OrderByType.Desc, true)]
public class GroupRequest
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    [Required] public int RequestId { get; set; }

    [StringLength(10)]
    [SugarColumn(Length = 10)]
    public string UserFromId { get; set; }

    [StringLength(10)]
    [SugarColumn(Length = 10)]
    public string GroupId { get; set; }

    public DateTime RequestTime { get; set; }

    public string Message { get; set; }

    public string NickName { get; set; }

    public string Grouping { get; set; }

    public string Remark { get; set; }

    public bool IsAccept { get; set; }

    public bool IsSolved { get; set; }

    [SugarColumn(IsNullable = true)] public DateTime? SolveTime { get; set; }

    [StringLength(10)]
    [SugarColumn(IsNullable = true, Length = 10)]
    public string? AcceptByUserId { get; set; }
}