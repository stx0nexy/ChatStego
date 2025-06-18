namespace PixChat.Application.DTOs;

public class ChatDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public bool IsGroup { get; set; }
    public int CreatorId { get; set; }
    public List<int> ParticipantIds { get; set; }
}