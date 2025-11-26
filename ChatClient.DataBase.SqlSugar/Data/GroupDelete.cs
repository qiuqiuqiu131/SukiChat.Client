using SqlSugar;

namespace ChatClient.DataBase.SqlSugar.Data;

[SugarTable("GroupDeletes")]
[SugarIndex("unique_groupDelete_deleteId", nameof(DeleteId), OrderByType.Desc, true)]
public class GroupDelete
{
    [SugarColumn(IsPrimaryKey = true)] public int DeleteId { get; set; }

    public string GroupId { get; set; }

    public string MemberId { get; set; }

    public int DeleteMethod { get; set; }

    public string OperateUserId { get; set; }

    public DateTime DeleteTime { get; set; }
}