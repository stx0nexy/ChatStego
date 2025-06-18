namespace PixChat.Application.Requests;

public class Verify2FARequest
{
    public string Email { get; set; }
    public string Code { get; set; }
}