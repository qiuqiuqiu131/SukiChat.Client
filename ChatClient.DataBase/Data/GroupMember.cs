using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatClient.DataBase.Data;

public class GroupMember
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [StringLength(10)] public string GroupId { get; set; }

    [StringLength(10)] public string UserId { get; set; }

    public int Status { get; set; }

    public DateTime JoinTime { get; set; }

    [StringLength(30)] public string? NickName { get; set; }

    public int HeadIndex { get; set; } = -1;
}