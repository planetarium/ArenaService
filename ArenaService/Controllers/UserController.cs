namespace ArenaService.Controllers;

using ArenaService.Dtos;
using ArenaService.Extensions;
using ArenaService.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

[Route("users")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly IUserRepository _userRepo;

    public UserController(IUserRepository userRepo)
    {
        _userRepo = userRepo;
    }

    [HttpPost]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
    public async Task<Results<NotFound<string>, Conflict<string>, Created>> Register(
        [FromBody] UserRegisterRequest userRegisterRequest
    )
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();
        var agentAddress = HttpContext.User.RequireAgentAddress();

        await _userRepo.AddOrGetUserAsync(
            agentAddress,
            avatarAddress,
            userRegisterRequest.NameWithHash,
            userRegisterRequest.PortraitId,
            userRegisterRequest.Cp,
            userRegisterRequest.Level
        );

        return TypedResults.Created();
    }
}
