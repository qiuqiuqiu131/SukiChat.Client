using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.DataBase.EfCore.Data;

[PrimaryKey(nameof(UserId), nameof(ChatPrivateId))]
[Table("ChatPrivateDetails")]
public class ChatPrivateDetail
{
    public string UserId { get; set; }

    public int ChatPrivateId { get; set; }

    public bool IsDeleted { get; set; }
}