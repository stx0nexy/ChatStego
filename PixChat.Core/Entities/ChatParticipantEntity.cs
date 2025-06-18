namespace PixChat.Core.Entities;

public class ChatParticipantEntity
{
    public int Id { get; set; }
    public int ChatId { get; set; }
    public ChatEntity Chat { get; set; }
    public int UserId { get; set; }
    public UserEntity User { get; set; }
    public bool IsAdmin { get; set; }
    public DateTime JoinedAt { get; set; }
}