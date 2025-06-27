using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.DataBase.EfCore.Data;

[PrimaryKey(nameof(UserId), nameof(ChatGroupId))]
[Table("ChatGroupDetails")]
public class ChatGroupDetail
{
    public string UserId { get; set; }

    public int ChatGroupId { get; set; }

    public bool IsDeleted { get; set; }
}