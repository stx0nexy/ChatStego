using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PixChat.Application.DTOs;
using PixChat.Application.Interfaces.Services;
using PixChat.Application.Requests;

namespace PixChat.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IMapper _mapper;
    private readonly ISteganographyService _steganographyService;
    private readonly IKeyService _keyService;
    private readonly IOfflineMessageService _offlineMessageService;
    private readonly IOneTimeMessageService _oneTimeMessageService;

    public UsersController(IUserService userService, IMapper mapper, ISteganographyService steganographyService,
        IKeyService keyService,
     IOfflineMessageService offlineMessageService, IOneTimeMessageService oneTimeMessageService)
    {
        _userService = userService;
        _mapper = mapper;
        _steganographyService = steganographyService;
        _keyService = keyService;
        _offlineMessageService = offlineMessageService;
        _oneTimeMessageService = oneTimeMessageService;
    }

    [Authorize]
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUserById(int userId)
    {
        var user = await _userService.GetByIdAsync(userId);
        if (user == null)
            return NotFound();
            
        return Ok(user);
    }
    
    [Authorize]
    [HttpGet("{userEmail}/email")]
    public async Task<IActionResult> GetUserByEmail(string userEmail)
    {
        var user = await _userService.GetByEmailAsync(userEmail);
        if (user == null)
            return NotFound();
        
        return Ok(user);
    }
    

    [HttpPost]
    public async Task<IActionResult> AddUser([FromBody] UserDto request)
    {
        await _userService.AddAsync(request);
        return Ok();
    }

    [Authorize]
    [HttpPut("{userId}")]
    public async Task<IActionResult> UpdateUser(int userId, [FromBody] UserDto request)
    {
        await _userService.UpdateAsync(request);
        return Ok();
    }

    [Authorize]
    [HttpDelete("{userId}")]
    public async Task<IActionResult> DeleteUser(int userId)
    {
        await _userService.DeleteAsync(userId);
        return Ok();
    }
    
    [Authorize]
    [HttpPost("{userId}/receiveMessage")]
    public async Task<IActionResult> ReceiveMessage([FromBody] MessageRequest request)
    {
        try
        {
            byte[] image = Convert.FromBase64String(request.Base64Image);
            var (message, timestamp, encryptedAesKey, aesIv ) =
                _steganographyService.ExtractFullMessage(image, request.EncryptedKey);

            return Ok(new
            {
                message,
                timestamp,
                encryptedAesKey,
                aesIv
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
    
    [HttpPost("{userId}/uploadPhoto")]
    public async Task<IActionResult> UploadUserProfilePicture(int userId, IFormFile image)
    {
        using (var stream = image.OpenReadStream())
        {
            var userDto = await _userService.UploadUserProfilePictureAsync(userId, stream, image.FileName);
            return Ok(userDto);
        }
    }

    [Authorize]
    [HttpPost("confirmMessageReceived")]
    public async Task<IActionResult> ConfirmMessageReceived([FromBody] MessageConfirmRequest request)
    {
        try
        {
            await _offlineMessageService.MarkMessageAsReceivedAsync(request.MessageId);
            await _offlineMessageService.DeleteMessageAsync(request.MessageId);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
    
    [Authorize]
    [HttpPost("confirmOneTimeMessageReceived")]
    public async Task<IActionResult> ConfirmOneTimeMessageReceived([FromBody] MessageConfirmRequest request)
    {
        try
        {
            await _oneTimeMessageService.MarkOneTimeMessageAsReceivedAsync(request.MessageId);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }


}