using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChatClient.DataBase.Data;

public class FriendDelete
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int DeleteId { get; set; }

    public string UseId1 { get; set; }

    public string UserId2 { get; set; }

    public DateTime DeleteTime { get; set; }
}