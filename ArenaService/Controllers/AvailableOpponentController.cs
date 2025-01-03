namespace ArenaService.Controllers;

using ArenaService.Dtos;
using ArenaService.Extensions;
using ArenaService.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

[Route("seasons/{seasonId}/available-opponents")]
[ApiController]
public class AvailableOpponentController : ControllerBase
{
    private readonly IAvailableOpponentRepository _availableOpponentRepo;
    private readonly IParticipantRepository _participantRepo;

    public AvailableOpponentController(
        IAvailableOpponentRepository availableOpponentRepo,
        IParticipantRepository participantRepo
    )
    {
        _availableOpponentRepo = availableOpponentRepo;
        _participantRepo = participantRepo;
    }

    [HttpGet]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [ProducesResponseType(typeof(AvailableOpponentsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(UnauthorizedHttpResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(NotFound<string>), StatusCodes.Status404NotFound)]
    public async Task<
        Results<UnauthorizedHttpResult, NotFound<string>, Ok<AvailableOpponentsResponse>>
    > GetAvailableOpponents(int seasonId)
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var participant = await _participantRepo.GetParticipantByAvatarAddressAsync(
            seasonId,
            avatarAddress
        );

        if (participant is null)
        {
            return TypedResults.NotFound("Not participant user.");
        }

        var availableOpponents = await _availableOpponentRepo.GetAvailableOpponents(participant.Id);
        var opponents = availableOpponents.Select(ao => ao.Opponent).ToList();

        return TypedResults.Ok(
            new AvailableOpponentsResponse { AvailableOpponents = opponents.ToResponse() }
        );
    }

    [HttpPost]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(UnauthorizedHttpResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(NotFound<string>), StatusCodes.Status404NotFound)]
    public async Task<Results<UnauthorizedHttpResult, NotFound<string>, Created>> ResetOpponents(
        int seasonId
    )
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        // Dummy implementation
        return TypedResults.Created();
    }
}
