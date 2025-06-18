namespace PixChat.Application.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string PasswordHash { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string Username { get; set; }
    public bool Status { get; set; }
    public bool IsVerified { get; set; }
    public DateTime LastSeen { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}