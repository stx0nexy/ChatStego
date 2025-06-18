namespace PixChat.Core.Entities;

public class ContactEntity
{
    public int Id { get; set; }
    public int UserId { get; set; } 
    public int ContactUserId { get; set; } 

    public bool IsBlockedByUser { get; set; } 
    public bool IsBlockedByContact { get; set; } 

    public UserEntity User { get; set; } 
    public UserEntity ContactUser { get; set; } 
}
