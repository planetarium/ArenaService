namespace ArenaService.Controllers;

using System.Security.Claims;
using ArenaService.Dtos;
using ArenaService.Extensions;
using ArenaService.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

[Route("seasons/{seasonId}/available-opponents")]
[ApiController]
public class AvailableOpponentController : ControllerBase
{
    private readonly AvailableOpponentService _availableOpponentService;
    private readonly ParticipantService _participantService;

    public AvailableOpponentController(
        AvailableOpponentService availableOpponentService,
        ParticipantService participantService
    )
    {
        _availableOpponentService = availableOpponentService;
        _participantService = participantService;
    }

    private string? ExtractAvatarAddress()
    {
        if (HttpContext.User.Identity is ClaimsIdentity identity)
        {
            var claim = identity.FindFirst("avatar");
            return claim?.Value;
        }
        return null;
    }

    [HttpGet]
    public async Task<
        Results<UnauthorizedHttpResult, NotFound<string>, Ok<AvailableOpponentsResponse>>
    > GetAvailableOpponents(int seasonId)
    {
        var avatarAddress = ExtractAvatarAddress();

        if (avatarAddress is null)
        {
            return TypedResults.Unauthorized();
        }

        var participant = await _participantService.GetParticipantByAvatarAddressAsync(
            seasonId,
            avatarAddress
        );

        if (participant is null)
        {
            return TypedResults.NotFound("Not participant user.");
        }

        var opponents = await _availableOpponentService.GetAvailableOpponents(participant.Id);

        return TypedResults.Ok(
            new AvailableOpponentsResponse { AvailableOpponents = opponents.ToResponse() }
        );
    }

    [HttpGet]
    public async Task<Results<UnauthorizedHttpResult, NotFound<string>, Created>> ResetOpponents(
        int seasonId
    )
    {
        var avatarAddress = ExtractAvatarAddress();

        if (avatarAddress is null)
        {
            return TypedResults.Unauthorized();
        }

        // Dummy implementation
        return TypedResults.Created();
    }
}
