using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using PixChat.Application.Services;
using PixChat.Application.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System;

namespace PixChat.Tests;

public class JwtTokenServiceTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly JwtTokenService _jwtTokenService;

    public JwtTokenServiceTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();

        _mockConfiguration.Setup(c => c["Jwt:Key"]).Returns("ThisIsASecretKeyForTestingPurposesThatIsAtLeast32BytesLong");
        _mockConfiguration.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        _mockConfiguration.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");

        _jwtTokenService = new JwtTokenService(_mockConfiguration.Object);
    }

    [Fact]
    public void GenerateToken_ReturnsValidToken()
    {
        // Arrange
        var user = new UserDto
        {
            Id = 1,
            Email = "test@example.com",
            Username = "testuser"
        };

        // Act
        var tokenString = _jwtTokenService.GenerateToken(user);

        // Assert
        Assert.NotNull(tokenString);
        Assert.NotEmpty(tokenString);

        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_mockConfiguration.Object["Jwt:Key"])),
            ValidateIssuer = true,
            ValidIssuer = _mockConfiguration.Object["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = _mockConfiguration.Object["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        SecurityToken validatedToken;
        ClaimsPrincipal principal;
        try
        {
            principal = tokenHandler.ValidateToken(tokenString, validationParameters, out validatedToken);
        }
        catch (Exception ex)
        {
            throw new Xunit.Sdk.XunitException($"Token validation failed: {ex.Message}", ex);
        }

        Assert.NotNull(principal);
        Assert.True(principal.Identity.IsAuthenticated);
        
        Assert.Contains(principal.Claims, c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" && c.Value == user.Email);

        Assert.Contains(principal.Claims, c => c.Type == "id" && c.Value == user.Id.ToString());
        Assert.Contains(principal.Claims, c => c.Type == "username" && c.Value == user.Username);

        var jwtToken = validatedToken as JwtSecurityToken;
        Assert.NotNull(jwtToken);
        Assert.Equal(_mockConfiguration.Object["Jwt:Issuer"], jwtToken.Issuer);
        Assert.Contains(_mockConfiguration.Object["Jwt:Audience"], jwtToken.Audiences);
        Assert.True(jwtToken.ValidTo > DateTime.UtcNow);
    }

    [Fact]
    public void GenerateToken_ReturnsTokenWithCorrectClaims()
    {
        // Arrange
        var user = new UserDto
        {
            Id = 42,
            Email = "another@example.com",
            Username = "anotheruser"
        };

        // Act
        var tokenString = _jwtTokenService.GenerateToken(user);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(tokenString);

        Assert.Contains(jwtToken.Claims, c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Email);
        Assert.Contains(jwtToken.Claims, c => c.Type == "id" && c.Value == user.Id.ToString());
        Assert.Contains(jwtToken.Claims, c => c.Type == "username" && c.Value == user.Username);
    }

    [Fact]
    public void GenerateToken_HasExpirationTime()
    {
        // Arrange
        var user = new UserDto
        {
            Id = 1,
            Email = "test@example.com",
            Username = "testuser"
        };

        // Act
        var tokenString = _jwtTokenService.GenerateToken(user);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(tokenString);

        Assert.NotNull(jwtToken.ValidTo);
        Assert.True(jwtToken.ValidTo > DateTime.UtcNow.AddHours(2.9) && jwtToken.ValidTo < DateTime.UtcNow.AddHours(3.1));
    }
}