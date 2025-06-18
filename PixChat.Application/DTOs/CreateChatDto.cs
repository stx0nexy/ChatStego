namespace PixChat.Application.DTOs;

public class CreateChatDto
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public bool IsGroup { get; set; }
    public int CreatorId { get; set; }
    public List<int> ParticipantIds { get; set; }
}