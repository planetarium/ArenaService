namespace ArenaService.Controllers;

using ArenaService.Constants;
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
    private readonly IParticipateService _participateService;
    private readonly IRefreshPriceRepository _refreshPriceRepository;
    private readonly ISpecifyOpponentsService _specifyOpponentsService;

    public AvailableOpponentController(
        IAvailableOpponentRepository availableOpponentRepo,
        IParticipantRepository participantRepo,
        ISeasonCacheRepository seasonCacheRepo,
        IParticipateService participateService,
        ISpecifyOpponentsService specifyOpponentsService,
        IRefreshPriceRepository refreshPriceRepository,
        IBackgroundJobClient jobClient
    )
    {
        _availableOpponentRepo = availableOpponentRepo;
        _participantRepo = participantRepo;
        _seasonCacheRepo = seasonCacheRepo;
        _participateService = participateService;
        _specifyOpponentsService = specifyOpponentsService;
        _refreshPriceRepository = refreshPriceRepository;
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
        // var avatarAddress = HttpContext.User.RequireAvatarAddress();

        // var currentSeason = await _seasonCacheRepo.GetSeasonAsync();
        // var currentRound = await _seasonCacheRepo.GetRoundAsync();

        // if (currentSeason is null || currentRound is null)
        // {
        //     return TypedResults.StatusCode(StatusCodes.Status503ServiceUnavailable);
        // }
        // await _participateService.ParticipateAsync(currentSeason.Value.Id, avatarAddress);

        // var availableOpponents = await _availableOpponentRepo.GetAvailableOpponents(
        //     avatarAddress,
        //     currentRound.Value.Id
        // );

        // if (availableOpponents == null)
        // {
        //     return TypedResults.NotFound("No available opponents found for the current round.");
        // }

        // var opponents = new List<Participant>();

        // foreach (var oaa in availableOpponents.OpponentAvatarAddresses)
        // {
        //     var participant = await _participantRepo.GetParticipantAsync(
        //         currentSeason.Value.Id,
        //         new Address(oaa)
        //     );
        //     if (participant != null)
        //     {
        //         opponents.Add(participant);
        //     }
        // }

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

        _jobClient.Enqueue<RefreshProcessor>(processor =>
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
    [SwaggerOperation(Summary = "", Description = "")]
    [SwaggerResponse(
        StatusCodes.Status200OK,
        "AvailableOpponents",
        typeof(AvailableOpponentsResponse)
    )]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "")]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "")]
    [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "")]
    public async Task<
        Results<
            UnauthorizedHttpResult,
            NotFound<string>,
            StatusCodeHttpResult,
            BadRequest<string>,
            Ok<AvailableOpponentsResponse>
        >
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

        var refreshRequestsCount = await _availableOpponentRepo.GetRefreshRequestCount(
            avatarAddress,
            currentRound.Value.Id
        );
        var refreshPrice = await _refreshPriceRepository.GetPriceAsync(
            currentSeason.Value.Id,
            refreshRequestsCount
        );

        if (refreshPrice.Price != 0)
        {
            return TypedResults.BadRequest(
                "Free refresh is not available at this time. Additional cost is required."
            );
        }

        var opponents = await _specifyOpponentsService.SpecifyOpponentsAsync(
            avatarAddress,
            currentSeason.Value.Id,
            currentRound.Value.Id
        );

        var refreshRequest = await _availableOpponentRepo.AddRefreshRequest(
            currentSeason.Value.Id,
            currentRound.Value.Id,
            avatarAddress,
            refreshPrice.DetailId,
            null,
            null,
            opponents.Select(o => o.AvatarAddress).ToList()
        );
        await _availableOpponentRepo.AddAvailableOpponents(
            currentSeason.Value.Id,
            currentRound.Value.Id,
            avatarAddress,
            refreshRequest.Id,
            opponents.Select(o => (o.AvatarAddress, o.GroupId)).ToList()
        );

        var availableOpponentsResponse = new List<AvailableOpponentResponse>();

        foreach (var opponent in opponents)
        {
            var participant = await _participantRepo.GetParticipantAsync(
                currentSeason.Value.Id,
                opponent.AvatarAddress
            );

            availableOpponentsResponse.Add(
                new AvailableOpponentResponse
                {
                    AvatarAddress = participant.AvatarAddress,
                    NameWithHash = participant.User.NameWithHash,
                    PortraitId = participant.User.PortraitId,
                    Cp = participant.User.Cp,
                    Level = participant.User.Level,
                    SeasonId = participant.SeasonId,
                    Score = participant.Score,
                    Rank = opponent.Rank,
                    IsAttacked = false,
                    ScoreGainOnWin = OpponentGroupConstants.Groups[opponent.GroupId].WinScore,
                    ScoreLossOnLose = OpponentGroupConstants.Groups[opponent.GroupId].LoseScore,
                    IsVictory = null,
                    ClanImageURL = ""
                }
            );
        }

        return TypedResults.Ok(
            new AvailableOpponentsResponse
            {
                AvailableOpponents = availableOpponentsResponse,
                RefreshTxTrackingStatus = RefreshTxTrackingStatus.COMPLETED,
                TxStatus = null,
                TxId = null,
            }
        );
    }
}
