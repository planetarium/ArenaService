namespace ArenaService.Controllers;

using ArenaService.Dtos;
using ArenaService.Extensions;
using ArenaService.Repositories;
using ArenaService.Services;
using ArenaService.Worker;
using Hangfire;
using Libplanet.Crypto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

[Route("battle")]
[ApiController]
public class BattleController : ControllerBase
{
    private readonly IBackgroundJobClient _jobClient;
    private readonly IBattleLogRepository _battleLogRepo;
    private readonly ISeasonCacheRepository _seasonCacheRepo;
    private readonly ParticipateService _participateService;

    public BattleController(
        IBattleLogRepository battleLogRepo,
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
    [ProducesResponseType(typeof(BattleTokenResponse), StatusCodes.Status200OK)]
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

        var battleLog = await _battleLogRepo.AddBattleLogAsync(
            currentSeason.Value.Id,
            avatarAddress,
            defenderAvatarAddress,
            "token"
        );

        return TypedResults.Ok(
            new BattleTokenResponse { Token = battleLog.Token, BattleLogId = battleLog.Id }
        );
    }

    [HttpPost("request")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<Results<UnauthorizedHttpResult, NotFound<string>, Ok>> RequestBattle(
        [FromBody] string txId,
        [FromBody] int battleLogId
    )
    {
        var battleLog = await _battleLogRepo.GetBattleLogAsync(battleLogId);

        if (battleLog is null)
        {
            return TypedResults.NotFound($"Battle log with ID {battleLogId} not found.");
        }

        _jobClient.Enqueue<BattleTaskProcessor>(processor =>
            processor.ProcessAsync(txId, battleLogId)
        );

        return TypedResults.Ok();
    }

    [HttpGet("{battleLogId}")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [ProducesResponseType(typeof(BattleLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<
        Results<UnauthorizedHttpResult, NotFound<string>, Ok<BattleLogResponse>>
    > GetBattleLog(int battleLogId)
    {
        var battleLog = await _battleLogRepo.GetBattleLogAsync(battleLogId);

        if (battleLog is null)
        {
            return TypedResults.NotFound($"Battle log with ID {battleLogId} not found.");
        }

        return TypedResults.Ok(battleLog.ToResponse());
    }
}
