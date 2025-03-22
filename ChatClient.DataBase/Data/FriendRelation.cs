using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.DataBase.Data;

public class FriendRelation
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [StringLength(10)] public string User1Id { get; set; }

    [StringLength(10)] public string User2Id { get; set; }

    [Required] [StringLength(20)] public string Grouping { get; set; }

    [Required] public DateTime GroupTime { get; set; }

    public string? Remark { get; set; }

    public bool CantDisturb { get; set; } = false;

    public bool IsTop { get; set; } = false;

    public int LastChatId { get; set; }

    public bool IsChatting { get; set; } = true;
}