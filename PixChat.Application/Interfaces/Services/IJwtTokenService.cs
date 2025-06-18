using PixChat.Application.DTOs;

namespace PixChat.Application.Interfaces.Services;

public interface IJwtTokenService
{
    string GenerateToken(UserDto user);
}