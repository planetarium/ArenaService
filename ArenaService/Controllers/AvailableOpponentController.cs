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

[Route("seasons/{seasonId}/available-opponents")]
[ApiController]
public class AvailableOpponentController : ControllerBase
{
    private readonly IBackgroundJobClient _jobClient;
    private readonly IAvailableOpponentRepository _availableOpponentRepo;
    private readonly IParticipantRepository _participantRepo;
    private readonly ISeasonRepository _seasonRepo;

    public AvailableOpponentController(
        IAvailableOpponentRepository availableOpponentRepo,
        IParticipantRepository participantRepo,
        ISeasonRepository seasonRepo,
        IBackgroundJobClient jobClient
    )
    {
        _availableOpponentRepo = availableOpponentRepo;
        _participantRepo = participantRepo;
        _seasonRepo = seasonRepo;
        _jobClient = jobClient;
    }

    [HttpGet]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [ProducesResponseType(typeof(AvailableOpponentsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(UnauthorizedHttpResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(NotFound<string>), StatusCodes.Status404NotFound)]
    public async Task<
        Results<UnauthorizedHttpResult, NotFound<string>, Ok<AvailableOpponentsResponse>>
    > GetAvailableOpponents(int seasonId, long blockIndex)
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var season = await _seasonRepo.GetSeasonAsync(seasonId);

        if (season == null)
        {
            return TypedResults.NotFound("No season found.");
        }

        var currentArenaInterval = season.ArenaIntervals.FirstOrDefault(ai =>
            ai.StartBlock <= blockIndex && ai.EndBlock >= blockIndex
        );

        if (currentArenaInterval == null)
        {
            return TypedResults.NotFound(
                $"No active arena interval found for block index {blockIndex}."
            );
        }

        var availableOpponents = await _availableOpponentRepo.GetAvailableOpponents(
            avatarAddress,
            seasonId,
            currentArenaInterval.Id
        );

        if (availableOpponents == null)
        {
            return TypedResults.NotFound($"Available opponents not found.");
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
    [ProducesResponseType(typeof(NotFound<string>), StatusCodes.Status404NotFound)]
    public Results<UnauthorizedHttpResult, NotFound<string>, Ok> RequestResetOpponents(int seasonId)
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        _jobClient.Enqueue<CalcAvailableOpponentsProcessor>(processor =>
            processor.ProcessAsync(avatarAddress, seasonId)
        );

        return TypedResults.Ok();
    }
}
