using System.Security.Claims;
using Libplanet.Crypto;

namespace ArenaService.Shared.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Address RequireAvatarAddress(this ClaimsPrincipal user)
    {
        var address =
            user?.Claims.FirstOrDefault(c => c.Type == "avatar_address")?.Value
            ?? throw new UnauthorizedAccessException(
                "Avatar address is required but not provided."
            );

        return new Address(address);
    }

    public static Address RequireAgentAddress(this ClaimsPrincipal user)
    {
        var address =
            user?.Claims.FirstOrDefault(c => c.Type == "address")?.Value
            ?? throw new UnauthorizedAccessException("Agent address is required but not provided.");

        return new Address(address);
    }
}
