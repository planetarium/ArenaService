using System.Security.Claims;

namespace ArenaService.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string RequireAvatarAddress(this ClaimsPrincipal user)
    {
        return user?.Claims.FirstOrDefault(c => c.Type == "avatar_address")?.Value
            ?? throw new UnauthorizedAccessException(
                "Avatar address is required but not provided."
            );
    }
}
