using SqlSugar;

namespace ChatClient.DataBase.SqlSugar.Data;

[SugarTable("Users")]
public class User
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public string Id { get; set; }

    public string Name { get; set; }

    public bool isMale { get; set; }

    [SugarColumn(IsNullable = true)] public DateTime? Birthday { get; set; }

    [SugarColumn(IsNullable = true, Length = 100)]
    public string? Introduction { get; set; }

    [SugarColumn(DefaultValue = "-1")] public int HeadIndex { get; set; } = -1;

    public int HeadCount { get; set; }

    [SugarColumn(DefaultValue = "datatime('1900-01-01 00:00:00')")]
    public DateTime LastReadFriendMessageTime { get; set; } = DateTime.MinValue;

    [SugarColumn(DefaultValue = "datatime('1900-01-01 00:00:00')")]
    public DateTime LastReadGroupMessageTime { get; set; } = DateTime.MinValue;

    [SugarColumn(DefaultValue = "datatime('1900-01-01 00:00:00')")]
    public DateTime LastDeleteFriendMessageTime { get; set; } = DateTime.MinValue;

    [SugarColumn(DefaultValue = "datatime('1900-01-01 00:00:00')")]
    public DateTime LastDeleteGroupMessageTime { get; set; } = DateTime.MinValue;

    public DateTime RegisteTime { get; set; }
}