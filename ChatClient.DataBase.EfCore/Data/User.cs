using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatClient.DataBase.EfCore.Data;

[Table("Users")]
public class User
{
    [Key] public string Id { get; set; }

    [Required] public string Name { get; set; }

    [Required] public bool isMale { get; set; }

    public DateTime? Birthday { get; set; }

    [StringLength(100)] public string? Introduction { get; set; }

    public int HeadIndex { get; set; } = -1;

    public int HeadCount { get; set; }


    public DateTime LastReadFriendMessageTime { get; set; } = DateTime.MinValue;


    public DateTime LastReadGroupMessageTime { get; set; } = DateTime.MinValue;

    public DateTime LastDeleteFriendMessageTime { get; set; } = DateTime.MinValue;

    public DateTime LastDeleteGroupMessageTime { get; set; } = DateTime.MinValue;

    [Required] public DateTime RegisteTime { get; set; }
}