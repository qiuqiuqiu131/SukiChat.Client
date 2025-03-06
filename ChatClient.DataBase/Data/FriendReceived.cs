using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ChatClient.DataBase.Data;

public class FriendReceived
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    public int RequestId { get; set; }
    
    public string UserFromId { get; set; }
    
    public string UserTargetId { get; set; }
    
    public DateTime ReceiveTime { get; set; }
    
    public bool IsAccept { get; set; }
    
    public bool IsSolved { get; set; }
    
    public DateTime SolveTime { get; set; }
}