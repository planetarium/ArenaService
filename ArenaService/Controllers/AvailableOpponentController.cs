namespace ArenaService.Controllers;

using ArenaService.Dtos;
using ArenaService.Extensions;
using ArenaService.Models;
using ArenaService.Repositories;
using ArenaService.Services;
using ArenaService.Worker;
using Hangfire;
using Libplanet.Crypto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

[Route("available-opponents")]
[ApiController]
public class AvailableOpponentController : ControllerBase
{
    private readonly IBackgroundJobClient _jobClient;
    private readonly IAvailableOpponentRepository _availableOpponentRepo;
    private readonly IParticipantRepository _participantRepo;
    private readonly ISeasonCacheRepository _seasonCacheRepo;
    private readonly ParticipateService _participateService;

    public AvailableOpponentController(
        IAvailableOpponentRepository availableOpponentRepo,
        IParticipantRepository participantRepo,
        ISeasonCacheRepository seasonCacheRepo,
        ParticipateService participateService,
        IBackgroundJobClient jobClient
    )
    {
        _availableOpponentRepo = availableOpponentRepo;
        _participantRepo = participantRepo;
        _seasonCacheRepo = seasonCacheRepo;
        _participateService = participateService;
        _jobClient = jobClient;
    }

    [HttpGet]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [SwaggerOperation(Summary = "", Description = "")]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "AvailableOpponents",
        typeof(AvailableOpponentsResponse)
    )]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Status401Unauthorized")]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "Status503ServiceUnavailable")]
    public async Task<
        Results<UnauthorizedHttpResult, NotFound<string>, StatusCodeHttpResult, Ok>
    > GetAvailableOpponents()
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var currentSeason = await _seasonCacheRepo.GetSeasonAsync();
        var currentRound = await _seasonCacheRepo.GetRoundAsync();

        if (currentSeason is null || currentRound is null)
        {
            return TypedResults.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
        await _participateService.ParticipateAsync(currentSeason.Value.Id, avatarAddress);

        var availableOpponents = await _availableOpponentRepo.GetAvailableOpponents(
            avatarAddress,
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
                currentSeason.Value.Id,
                new Address(oaa)
            );
            if (participant != null)
            {
                opponents.Add(participant);
            }
        }

        return TypedResults.Ok();
    }

    [HttpPost("refresh")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(UnauthorizedHttpResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<
        Results<UnauthorizedHttpResult, NotFound<string>, StatusCodeHttpResult, Ok>
    > RequestRefresh(string txId)
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var currentSeason = await _seasonCacheRepo.GetSeasonAsync();
        var currentRound = await _seasonCacheRepo.GetRoundAsync();

        if (currentSeason is null || currentRound is null)
        {
            return TypedResults.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        await _participateService.ParticipateAsync(currentSeason.Value.Id, avatarAddress);

        _jobClient.Enqueue<CalcAvailableOpponentsProcessor>(processor =>
            processor.ProcessAsync(
                avatarAddress,
                currentSeason.Value.Id,
                currentRound.Value.Id,
                null
            )
        );

        return TypedResults.Ok();
    }

    [HttpPost("free-refresh")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [ProducesResponseType(typeof(AvailableOpponentsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(UnauthorizedHttpResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<
        Results<UnauthorizedHttpResult, NotFound<string>, StatusCodeHttpResult, Ok>
    > RequestFreeRefresh()
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var currentSeason = await _seasonCacheRepo.GetSeasonAsync();
        var currentRound = await _seasonCacheRepo.GetRoundAsync();

        if (currentSeason is null || currentRound is null)
        {
            return TypedResults.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        await _participateService.ParticipateAsync(currentSeason.Value.Id, avatarAddress);

        _jobClient.Enqueue<CalcAvailableOpponentsProcessor>(processor =>
            processor.ProcessAsync(
                avatarAddress,
                currentSeason.Value.Id,
                currentRound.Value.Id,
                null
            )
        );

        return TypedResults.Ok();
    }
}
