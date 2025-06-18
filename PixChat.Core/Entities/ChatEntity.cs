namespace PixChat.Core.Entities;

public class ChatEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public bool IsGroup { get; set; }
    public int CreatorId { get; set; }
    public UserEntity Creator { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<ChatParticipantEntity> Participants { get; set; }
}