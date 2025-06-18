namespace PixChat.Application.Requests;

public class UpdateBlockStatusRequest
{
    public int UserId { get; set; }
    public int ContactUserId { get; set; }
    public bool IsBlocked { get; set; }
}