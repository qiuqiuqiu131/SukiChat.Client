using System.ComponentModel.DataAnnotations;

namespace ChatClient.DataBase.Data;

public class User
{
    [Key] public string Id { get; set; }

    [Required] public string Name { get; set; }

    [Required] public bool isMale { get; set; }

    public DateOnly? Birthday { get; set; }

    public string? Introduction { get; set; }

    public int HeadIndex { get; set; } = -1;

    public int HeadCount { get; set; }

    public DateTime LastReadFriendMessageTime { get; set; } = DateTime.MinValue;

    public DateTime LastReadGroupMessageTime { get; set; } = DateTime.MinValue;

    public DateTime LastDeleteFriendMessageTime { get; set; } = DateTime.MinValue;

    public DateTime LastDeleteGroupMessageTime { get; set; } = DateTime.MinValue;

    [Required] public DateTime RegisteTime { get; set; }
}