using SqlSugar;

namespace ChatClient.DataBase.SqlSugar.Data;

[SugarTable("ChatPrivates")]
[SugarIndex("unique_chatPrivate_chatId", nameof(ChatId), OrderByType.Desc, true)]
[SugarIndex("chatPrivate_userFromId_userTargetId", nameof(UserFromId), OrderByType.Asc,
    nameof(UserTargetId), OrderByType.Asc)]
public class ChatPrivate
{
    [SugarColumn(IsPrimaryKey = true)] public int ChatId { get; set; }

    [SugarColumn(Length = 10)] public string UserFromId { get; set; }

    [SugarColumn(Length = 10)] public string UserTargetId { get; set; }

    public string Message { get; set; }

    public DateTime Time { get; set; }

    public bool IsRetracted { get; set; }

    [SugarColumn(DefaultValue = "datatime('1900-01-01 00:00:00')")]
    public DateTime RetractedTime { get; set; } = DateTime.MinValue;
}