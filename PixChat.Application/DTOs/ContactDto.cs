namespace PixChat.Application.DTOs;

public class ContactDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ContactUserId { get; set; }
    public bool IsBlockedByUser { get; set; } 
    public bool IsBlockedByContact { get; set; } 
}