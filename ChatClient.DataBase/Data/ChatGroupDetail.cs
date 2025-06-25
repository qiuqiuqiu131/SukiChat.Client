using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SqlSugar;

namespace ChatClient.DataBase.Data;

[PrimaryKey(nameof(UserId), nameof(ChatGroupId))]
[Table("ChatGroupDetails")]
[SugarTable("ChatGroupDetails")]
[SugarIndex("unique_chatGroupDetail_userId_chatGroupId", nameof(UserId), OrderByType.Asc,
    nameof(ChatGroupId), OrderByType.Asc, true)]
public class ChatGroupDetail
{
    [SugarColumn(IsPrimaryKey = true)] public string UserId { get; set; }

    [SugarColumn(IsPrimaryKey = true)] public int ChatGroupId { get; set; }

    public bool IsDeleted { get; set; }
}