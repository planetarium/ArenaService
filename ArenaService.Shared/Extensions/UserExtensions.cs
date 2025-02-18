namespace ArenaService.Shared.Extensions;

using ArenaService.Shared.Dtos;
using ArenaService.Shared.Models;

public static class UserExtensions
{
    public static UserResponse ToResponse(this User user)
    {
        return new UserResponse
        {
            AvatarAddress = user.AvatarAddress,
            NameWithHash = user.NameWithHash,
            PortraitId = user.PortraitId,
            Cp = user.Cp,
            Level = user.Level,
        };
    }
}
