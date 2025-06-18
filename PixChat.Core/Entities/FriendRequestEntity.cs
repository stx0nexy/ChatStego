namespace PixChat.Core.Entities;

public class FriendRequestEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ContactUserId { get; set; }
    public bool Status  { get; set; }
    public bool Received { get; set; } = false;
}