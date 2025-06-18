namespace PixChat.Core.Entities;

public class UserKeyEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string PublicKey { get; set; }
    
    public UserEntity User { get; set; }

}