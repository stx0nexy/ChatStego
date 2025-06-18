namespace PixChat.Application.DTOs;

public class LoginResponseDto
{
    public string token { get; set; }
    public UserLoginInfo user { get; set; }
}