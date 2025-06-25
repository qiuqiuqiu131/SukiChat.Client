using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SqlSugar;

namespace ChatClient.DataBase.Data;

[Table("FriendRequests")]
[SugarTable("FriendRequests")]
[SugarIndex("unique_friendRequest_requestId", nameof(RequestId), OrderByType.Desc, true)]
public class FriendRequest
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    public int RequestId { get; set; }

    [StringLength(10)]
    [SugarColumn(Length = 10)]
    public string UserFromId { get; set; }

    [StringLength(10)]
    [SugarColumn(Length = 10)]
    public string UserTargetId { get; set; }

    public string Message { get; set; }

    public string Group { get; set; }

    public string Remark { get; set; }

    public DateTime RequestTime { get; set; }

    public bool IsAccept { get; set; }

    public bool IsSolved { get; set; }

    [SugarColumn(IsNullable = true)] public DateTime? SolveTime { get; set; }
}