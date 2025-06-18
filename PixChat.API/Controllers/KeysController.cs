using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixChat.Application.DTOs;
using PixChat.Application.Interfaces.Services;

namespace PixChat.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class KeysController : ControllerBase
    {
        private readonly IKeyService _keyService;

        public KeysController(IKeyService keyService)
        {
            _keyService = keyService;
        }

        [HttpGet("public/{userId}")]
        public async Task<IActionResult> GetPublicKey(int userId)
        {
            try
            {
                var publicKey = await _keyService.GetPublicKeyAsync(userId);
                if (publicKey == null)
                {
                    return NotFound(new { message = $"Public key not found for user {userId}." });
                }
                return Ok(new { PublicKey = publicKey });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error while retrieving public key.", error = ex.Message });
            }
        }

        [HttpPost("public/{userId}")]
        public async Task<IActionResult> SavePublicKey(int userId, [FromBody] SavePublicKeyRequest request)
        {
            try
            {
                await _keyService.SavePublicKeyAsync(userId, request.PublicKey);

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error saving public key", error = ex.Message });
            }
        }
    }
}