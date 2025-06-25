using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SqlSugar;

namespace ChatClient.DataBase.Data;

[Table("Users")]
[SugarTable("Users")]
public class User
{
    [Key]
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public string Id { get; set; }

    [Required] public string Name { get; set; }

    [Required] public bool isMale { get; set; }

    [SugarColumn(IsNullable = true)] public DateTime? Birthday { get; set; }

    [SugarColumn(IsNullable = true, Length = 100)]
    [StringLength(100)]
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

    [Required] public DateTime RegisteTime { get; set; }
}