using SqlSugar;

namespace ChatClient.DataBase.SqlSugar.Data;

[SugarTable("GroupRequests")]
[SugarIndex("unique_groupRequest_requestId", nameof(RequestId), OrderByType.Desc, true)]
public class GroupRequest
{
    [SugarColumn(IsPrimaryKey = true)] public int RequestId { get; set; }

    [SugarColumn(Length = 10)] public string UserFromId { get; set; }

    [SugarColumn(Length = 10)] public string GroupId { get; set; }

    public DateTime RequestTime { get; set; }

    public string Message { get; set; }

    public string NickName { get; set; }

    public string Grouping { get; set; }

    public string Remark { get; set; }

    public bool IsAccept { get; set; }

    public bool IsSolved { get; set; }

    [SugarColumn(IsNullable = true)] public DateTime? SolveTime { get; set; }

    [SugarColumn(IsNullable = true, Length = 10)]
    public string? AcceptByUserId { get; set; }
}