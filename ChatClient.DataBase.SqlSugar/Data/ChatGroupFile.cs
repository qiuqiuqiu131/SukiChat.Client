using SqlSugar;

namespace ChatClient.DataBase.SqlSugar.Data;

[SugarTable("ChatGroupFiles")]
[SugarIndex("unique_chatGroupFile_chatId_userId", nameof(ChatId), OrderByType.Asc,
    nameof(UserId), OrderByType.Asc, true)]
public class ChatGroupFile
{
    [SugarColumn(IsPrimaryKey = true)] public int ChatId { get; set; }

    [SugarColumn(IsPrimaryKey = true)] public string UserId { get; set; }

    public string TargetPath { get; set; }
}