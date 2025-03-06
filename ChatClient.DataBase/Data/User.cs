using System.ComponentModel.DataAnnotations;

namespace ChatClient.DataBase.Data;

public class User
{
    [Key]
    public string Id { get; set; }
    
    [Required]
    public string Name { get; set; }
    
    [Required]
    public bool isMale { get; set; }
    
    public DateTime Birthday { get; set; }
    
    [Required]
    public string Password { get; set; }
    
    public string Introduction { get; set; }
    
    public int HeadIndex { get; set; }

    public int HeadCount { get; set; }
    
    [Required]
    public DateTime RegisteTime { get; set; }

    public void Copy(User other)
    {
        this.Name = other.Name;
        this.Password = other.Password;
        this.HeadCount = other.HeadCount;
        this.HeadIndex = other.HeadIndex;
        this.Introduction = other.Introduction;
    }
}