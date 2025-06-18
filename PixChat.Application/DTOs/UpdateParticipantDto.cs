namespace PixChat.Application.DTOs;

public class UpdateParticipantDto
{
    public int Id { get; set; }
    public bool IsAdmin { get; set; }
    public int ChatId { get; set; }
}