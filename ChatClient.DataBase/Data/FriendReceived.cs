using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SqlSugar;

namespace ChatClient.DataBase.Data;

[Table("FriendReceiveds")]
[SugarTable("FriendReceiveds")]
[SugarIndex("unique_friendRequest_requestId", nameof(RequestId), OrderByType.Desc, true)]
public class FriendReceived
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    public int RequestId { get; set; }

    public string UserFromId { get; set; }

    public string UserTargetId { get; set; }

    public DateTime ReceiveTime { get; set; }

    public string Message { get; set; }

    public bool IsAccept { get; set; }

    public bool IsSolved { get; set; }

    [SugarColumn(IsNullable = true)] public DateTime? SolveTime { get; set; }
}