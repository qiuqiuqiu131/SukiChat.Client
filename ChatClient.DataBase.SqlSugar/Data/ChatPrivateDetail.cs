using SqlSugar;

namespace ChatClient.DataBase.SqlSugar.Data;

[SugarTable("ChatPrivateDetails")]
[SugarIndex("unique_chatPrivateDetail_userId_chatPrivateId", nameof(UserId), OrderByType.Asc,
    nameof(ChatPrivateId), OrderByType.Asc, true)]
public class ChatPrivateDetail
{
    [SugarColumn(IsPrimaryKey = true)] public string UserId { get; set; }

    [SugarColumn(IsPrimaryKey = true)] public int ChatPrivateId { get; set; }

    public bool IsDeleted { get; set; }
}