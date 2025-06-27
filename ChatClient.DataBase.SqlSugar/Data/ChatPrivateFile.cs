using SqlSugar;

namespace ChatClient.DataBase.SqlSugar.Data;

[SugarTable("ChatPrivateFiles")]
[SugarIndex("unique_chatPrivateFile_chatId_userId", nameof(ChatId), OrderByType.Asc,
    nameof(UserId), OrderByType.Asc, true)]
public class ChatPrivateFile
{
    [SugarColumn(IsPrimaryKey = true)] public int ChatId { get; set; }

    [SugarColumn(IsPrimaryKey = true)] public string UserId { get; set; }

    public string TargetPath { get; set; }
}