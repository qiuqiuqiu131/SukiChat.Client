using SqlSugar;

namespace ChatClient.DataBase.SqlSugar.Data;

[SugarTable("GroupReceiveds")]
[SugarIndex("unique_groupReceived_requestId", nameof(RequestId), OrderByType.Desc, true)]
public class GroupReceived
{
    [SugarColumn(IsPrimaryKey = true)] public int RequestId { get; set; }

    [SugarColumn(Length = 10)] public string UserFromId { get; set; }

    [SugarColumn(Length = 10)] public string GroupId { get; set; }

    public DateTime ReceiveTime { get; set; }

    public string Message { get; set; }

    public bool IsAccept { get; set; }

    public bool IsSolved { get; set; }

    [SugarColumn(IsNullable = true)] public DateTime? SolveTime { get; set; }

    [SugarColumn(IsNullable = true, Length = 10)]
    public string? AcceptByUserId { get; set; }
}