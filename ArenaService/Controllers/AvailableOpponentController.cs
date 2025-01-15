namespace ArenaService.Controllers;

using ArenaService.Dtos;
using ArenaService.Extensions;
using ArenaService.Models;
using ArenaService.Repositories;
using ArenaService.Worker;
using Hangfire;
using Libplanet.Crypto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

[Route("available-opponents")]
[ApiController]
public class AvailableOpponentController : ControllerBase
{
    private readonly IBackgroundJobClient _jobClient;
    private readonly IAvailableOpponentRepository _availableOpponentRepo;
    private readonly IParticipantRepository _participantRepo;
    private readonly ISeasonCacheRepository _seasonCacheRepo;

    public AvailableOpponentController(
        IAvailableOpponentRepository availableOpponentRepo,
        IParticipantRepository participantRepo,
        ISeasonCacheRepository seasonCacheRepo,
        IBackgroundJobClient jobClient
    )
    {
        _availableOpponentRepo = availableOpponentRepo;
        _participantRepo = participantRepo;
        _seasonCacheRepo = seasonCacheRepo;
        _jobClient = jobClient;
    }

    [HttpGet]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [ProducesResponseType(typeof(AvailableOpponentsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<
        Results<
            UnauthorizedHttpResult,
            NotFound<string>,
            StatusCodeHttpResult,
            Ok<AvailableOpponentsResponse>
        >
    > GetAvailableOpponents()
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var currentSeason = await _seasonCacheRepo.GetSeasonAsync();
        var currentRound = await _seasonCacheRepo.GetRoundAsync();

        if (currentSeason is null || currentRound is null)
        {
            return TypedResults.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        var availableOpponents = await _availableOpponentRepo.GetAvailableOpponents(
            avatarAddress,
            currentSeason.Value.Id,
            currentRound.Value.Id
        );

        if (availableOpponents == null)
        {
            return TypedResults.NotFound("No available opponents found for the current round.");
        }

        var opponents = new List<Participant>();

        foreach (var oaa in availableOpponents.OpponentAvatarAddresses)
        {
            var participant = await _participantRepo.GetParticipantAsync(
                availableOpponents.SeasonId,
                new Address(oaa)
            );
            if (participant != null)
            {
                opponents.Add(participant);
            }
        }

        return TypedResults.Ok(
            new AvailableOpponentsResponse { AvailableOpponents = opponents.ToResponse() }
        );
    }

    [HttpPost]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(UnauthorizedHttpResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<
        Results<UnauthorizedHttpResult, NotFound<string>, StatusCodeHttpResult, Ok>
    > RequestResetOpponents()
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var currentSeason = await _seasonCacheRepo.GetSeasonAsync();
        var currentRound = await _seasonCacheRepo.GetRoundAsync();

        if (currentSeason is null || currentRound is null)
        {
            return TypedResults.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        _jobClient.Enqueue<CalcAvailableOpponentsProcessor>(processor =>
            processor.ProcessAsync(avatarAddress, currentSeason.Value.Id, currentRound.Value.Id)
        );

        return TypedResults.Ok();
    }
}
