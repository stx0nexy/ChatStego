namespace PixChat.Application.DTOs;

public class AddParticipantDto
{
    public int ChatId { get; set; }
    public int UserId { get; set; }
    public bool IsAdmin { get; set; }
}