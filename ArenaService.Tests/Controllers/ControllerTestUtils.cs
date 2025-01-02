namespace ArenaService.Tests.Utils;

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

public static class ControllerTestUtils
{
    public static void ConfigureMockHttpContextWithAuth(
        ControllerBase controller,
        string avatarAddress
    )
    {
        var user = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim("avatar_address", avatarAddress)], "mock")
        );

        var httpContext = new DefaultHttpContext { User = user };
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }
}
