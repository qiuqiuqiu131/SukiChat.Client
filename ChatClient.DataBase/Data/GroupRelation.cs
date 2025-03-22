using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatClient.DataBase.Data;

public class GroupRelation
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [StringLength(10)] public string GroupId { get; set; }

    [StringLength(10)] public string UserId { get; set; }

    public int Status { get; set; }

    [StringLength(20)] public string Grouping { get; set; }

    public DateTime JoinTime { get; set; }

    [StringLength(30)] public string? NickName { get; set; }

    [StringLength(30)] public string? Remark { get; set; }

    public bool CantDisturb { get; set; } = false;

    public bool IsTop { get; set; } = false;

    public int LastChatId { get; set; }

    public bool IsChatting { get; set; }
}