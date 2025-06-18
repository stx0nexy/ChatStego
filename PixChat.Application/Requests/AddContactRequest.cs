namespace PixChat.Application.Requests;

public class AddContactRequest
{
    public int UserId { get; set; }
    public int ContactUserId { get; set; }
    public bool IsBlocked { get; set; }
}