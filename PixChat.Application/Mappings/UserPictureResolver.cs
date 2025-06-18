using AutoMapper;
using Microsoft.Extensions.Options;
using PixChat.Application.Config;
using PixChat.Application.DTOs;
using PixChat.Core.Entities;

namespace PixChat.Application.Mappings;

public class UserPictureResolver : IMemberValueResolver<UserEntity, UserDto, string, object>
{
    private readonly ChatConfig _config;

    public UserPictureResolver(IOptionsSnapshot<ChatConfig> config)
    {
        _config = config.Value;
    }

    public object Resolve(UserEntity source, UserDto destination, string sourceMember, object destMember, ResolutionContext context)
    {
        if (string.IsNullOrEmpty(source.ProfilePictureFileName))
        {
            return null;
        }
        
        return $"{_config.CdnHost}/{_config.ImgUrl}/{source.ProfilePictureFileName}".Replace("\\", "/");
    }
}
