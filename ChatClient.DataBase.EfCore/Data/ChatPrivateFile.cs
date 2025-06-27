using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.DataBase.EfCore.Data;

[PrimaryKey(nameof(ChatId), nameof(UserId))]
[Table("ChatPrivateFiles")]
public class ChatPrivateFile
{
    public int ChatId { get; set; }

    public string UserId { get; set; }

    public string TargetPath { get; set; }
}