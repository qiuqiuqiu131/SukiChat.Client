using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.DataBase.EfCore.Data;

[Table("GroupMembers")]
[PrimaryKey(nameof(GroupId), nameof(UserId))]
public class GroupMember
{
    [StringLength(10)] public string GroupId { get; set; }

    [StringLength(10)] public string UserId { get; set; }

    public int Status { get; set; }

    public DateTime JoinTime { get; set; }

    [StringLength(30)] public string? NickName { get; set; }

    public int HeadIndex { get; set; } = -1;
}