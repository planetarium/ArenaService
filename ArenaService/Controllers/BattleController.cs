namespace ArenaService.Controllers;

using ArenaService.Dtos;
using ArenaService.Extensions;
using ArenaService.Repositories;
using ArenaService.Services;
using ArenaService.Worker;
using Hangfire;
using Libplanet.Crypto;
using Libplanet.Types.Tx;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

[Route("battle")]
[ApiController]
public class BattleController : ControllerBase
{
    private readonly IBackgroundJobClient _jobClient;
    private readonly IBattleRepository _battleLogRepo;
    private readonly ISeasonCacheRepository _seasonCacheRepo;
    private readonly ParticipateService _participateService;

    public BattleController(
        IBattleRepository battleLogRepo,
        ISeasonCacheRepository seasonCacheRepo,
        ParticipateService participateService,
        IBackgroundJobClient jobClient
    )
    {
        _battleLogRepo = battleLogRepo;
        _jobClient = jobClient;
        _seasonCacheRepo = seasonCacheRepo;
        _participateService = participateService;
    }

    [HttpGet("token")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [ProducesResponseType(typeof(BattleTokenResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(UnauthorizedHttpResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<
        Results<
            UnauthorizedHttpResult,
            NotFound<string>,
            StatusCodeHttpResult,
            Ok<BattleTokenResponse>
        >
    > CreateBattleToken(string opponentAvatarAddress)
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();

        var currentSeason = await _seasonCacheRepo.GetSeasonAsync();
        var currentRound = await _seasonCacheRepo.GetRoundAsync();

        if (currentSeason is null || currentRound is null)
        {
            return TypedResults.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        await _participateService.ParticipateAsync(currentSeason.Value.Id, avatarAddress);

        var defenderAvatarAddress = new Address(opponentAvatarAddress);

        var battleLog = await _battleLogRepo.AddBattleAsync(
            currentSeason.Value.Id,
            avatarAddress,
            defenderAvatarAddress,
            "token"
        );

        return TypedResults.Ok(
            new BattleTokenResponse { Token = battleLog.Token, BattleId = battleLog.Id }
        );
    }

    [HttpPost("{battleLogId}/request")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<Results<UnauthorizedHttpResult, NotFound<string>, Ok>> RequestBattle(
        int battleLogId,
        [FromBody] BattleRequest request
    )
    {
        var battleLog = await _battleLogRepo.GetBattleAsync(battleLogId);

        if (battleLog is null)
        {
            return TypedResults.NotFound($"Battle log with ID {battleLogId} not found.");
        }

        _jobClient.Enqueue<BattleProcessor>(processor =>
            processor.ProcessAsync(TxId.FromHex(request.TxId), battleLogId)
        );

        return TypedResults.Ok();
    }

    [HttpGet("{battleLogId}")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [ProducesResponseType(typeof(BattleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<
        Results<UnauthorizedHttpResult, NotFound<string>, Ok<BattleResponse>>
    > GetBattle(int battleLogId)
    {
        var battleLog = await _battleLogRepo.GetBattleAsync(battleLogId);

        if (battleLog is null)
        {
            return TypedResults.NotFound($"Battle log with ID {battleLogId} not found.");
        }

        return TypedResults.Ok(battleLog.ToResponse());
    }
}
