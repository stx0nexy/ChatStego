namespace PixChat.Application.DTOs;

public class ChatParticipantDto
{
    public int Id { get; set; }
    public int ChatId { get; set; }
    public int UserId { get; set; }
    public bool IsAdmin { get; set; }
    public DateTime JoinedAt { get; set; }
}