using SqlSugar;

namespace ChatClient.DataBase.SqlSugar.Data;

[SugarTable("FriendReceiveds")]
[SugarIndex("unique_friendRequest_requestId", nameof(RequestId), OrderByType.Desc, true)]
public class FriendReceived
{
    [SugarColumn(IsPrimaryKey = true)] public int RequestId { get; set; }

    public string UserFromId { get; set; }

    public string UserTargetId { get; set; }

    public DateTime ReceiveTime { get; set; }

    public string Message { get; set; }

    public bool IsAccept { get; set; }

    public bool IsSolved { get; set; }

    [SugarColumn(IsNullable = true)] public DateTime? SolveTime { get; set; }
}