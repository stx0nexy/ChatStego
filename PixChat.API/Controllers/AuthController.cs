using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PixChat.Application.DTOs;
using PixChat.Application.Interfaces.Services;
using PixChat.Application.Requests;

namespace PixChat.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IPasswordHasher<UserDto> _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUserService _userService;
    private readonly IEmailService _emailService;
    private readonly ITwoFactorService  _twoFactorService;

    public AuthController(IPasswordHasher<UserDto> passwordHasher,
        IJwtTokenService jwtTokenService, IUserService userService,
        IEmailService emailService, ITwoFactorService  twoFactorService)
    {
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _userService = userService;
        _emailService = emailService;
        _twoFactorService = twoFactorService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (await _userService.UserExistsByEmailAsync(request.Email))
        {
            return BadRequest("Email already exists");
        }

        var user = new UserDto()
        {
            Email = request.Email,
            Username = request.Username,
            Phone = request.Phone,
            IsVerified = false,
            Status = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        await _userService.AddAsync(user);

        await _twoFactorService.SendTwoFactorCodeAsync(user.Email);

        return Ok(new { message = "Verification code sent to your email" });
    }

    [HttpPost("verify-registration")]
    public async Task<IActionResult> VerifyRegistration([FromBody] Verify2FARequest request)
    {
        var isValid = _twoFactorService.VerifyCode(request.Email, request.Code);

        if (!isValid)
        {
            return Unauthorized("Invalid or expired verification code");
        }

        var user = await _userService.GetByEmailAsync(request.Email);
        if (user == null)
        {
            return NotFound("User not found");
        }

        user.IsVerified = true;
        await _userService.UpdateAsync(user);

        return Ok("Registration completed successfully");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userService.GetByEmailAsync(request.Email);
        if (user == null || !user.IsVerified)
        {
            return Unauthorized("User not verified");
        }

        if (_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password)
            != PasswordVerificationResult.Success)
        {
            return Unauthorized("Invalid credentials");
        }

        var token = _jwtTokenService.GenerateToken(user);

        return Ok(new 
        { 
            token, 
            user = new 
            { 
                user.Id, 
                user.Email, 
                user.Username 
            } 
        });
    }
}
