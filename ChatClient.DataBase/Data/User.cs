using System.ComponentModel.DataAnnotations;

namespace ChatClient.DataBase.Data;

public class User
{
    [Key] public string Id { get; set; }

    [Required] public string Name { get; set; }

    [Required] public bool isMale { get; set; }

    public DateTime? Birthday { get; set; }

    [Required] public string Password { get; set; }

    public string? Introduction { get; set; }

    public int HeadIndex { get; set; }

    public int HeadCount { get; set; }

    public DateTime LastReadFriendMessageTime { get; set; } = DateTime.MinValue;

    public DateTime LastReadGroupMessageTime { get; set; } = DateTime.MinValue;

    public DateTime LastDeleteFriendMessageTime { get; set; } = DateTime.MinValue;

    public DateTime LastDeleteGroupMessageTime { get; set; } = DateTime.MinValue;

    [Required] public DateTime RegisteTime { get; set; }

    public void Copy(User other)
    {
        Name = other.Name;
        Password = other.Password;
        HeadCount = other.HeadCount;
        LastDeleteFriendMessageTime = other.LastDeleteFriendMessageTime;
        LastDeleteGroupMessageTime = other.LastDeleteGroupMessageTime;
        LastReadFriendMessageTime = other.LastReadFriendMessageTime;
        LastReadGroupMessageTime = other.LastReadGroupMessageTime;
        HeadIndex = other.HeadIndex;
        Introduction = other.Introduction;
    }
}