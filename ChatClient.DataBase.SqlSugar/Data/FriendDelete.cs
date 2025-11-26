using SqlSugar;

namespace ChatClient.DataBase.SqlSugar.Data;

[SugarTable("FriendDeletes")]
[SugarIndex("unique_friendDelete_deleteId", nameof(DeleteId), OrderByType.Desc, true)]
public class FriendDelete
{
    [SugarColumn(IsPrimaryKey = true)] public int DeleteId { get; set; }

    public string UseId1 { get; set; }

    public string UserId2 { get; set; }

    public DateTime DeleteTime { get; set; }
}