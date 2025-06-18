using System.ComponentModel.DataAnnotations;

namespace PixChat.Application.Requests;

public class RegisterRequest
{
    [Required]
    public string Email { get; set; }

    [Required]
    public string Username { get; set; }

    [Required]
    public string Phone { get; set; }

    [Required]
    public string Password { get; set; }
    
}