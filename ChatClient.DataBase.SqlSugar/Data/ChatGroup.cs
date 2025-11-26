using SqlSugar;

namespace ChatClient.DataBase.SqlSugar.Data;

[SugarTable("ChatGroups")]
[SugarIndex("unique_chatGroup_chatId", nameof(ChatId), OrderByType.Desc, true)]
[SugarIndex("chatGroup_groupId", nameof(GroupId), OrderByType.Asc)]
public class ChatGroup
{
    [SugarColumn(IsPrimaryKey = true)] public int ChatId { get; set; }


    [SugarColumn(Length = 10)] public string UserFromId { get; set; }


    [SugarColumn(Length = 10)] public string GroupId { get; set; }

    public string Message { get; set; }

    public DateTime Time { get; set; }

    public bool IsRetracted { get; set; }


    [SugarColumn(DefaultValue = "datatime('1900-01-01 00:00:00')")]
    public DateTime RetractedTime { get; set; } = DateTime.MinValue;
}