namespace ArenaService.Controllers;

using ArenaService.Dtos;
using ArenaService.Extensions;
using ArenaService.Repositories;
using ArenaService.Worker;
using Hangfire;
using Libplanet.Crypto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

[Route("seasons/{seasonId}/battle")]
[ApiController]
public class BattleController : ControllerBase
{
    private readonly IBackgroundJobClient _jobClient;
    private readonly IAvailableOpponentRepository _availableOpponentRepo;
    private readonly IParticipantRepository _participantRepo;
    private readonly IBattleLogRepository _battleLogRepo;

    public BattleController(
        IAvailableOpponentRepository availableOpponentService,
        IParticipantRepository participantService,
        IBattleLogRepository battleLogRepo,
        IBackgroundJobClient jobClient
    )
    {
        _availableOpponentRepo = availableOpponentService;
        _participantRepo = participantService;
        _battleLogRepo = battleLogRepo;
        _jobClient = jobClient;
    }

    [HttpGet("token")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [ProducesResponseType(typeof(BattleTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(UnauthorizedHttpResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<
        Results<UnauthorizedHttpResult, NotFound<string>, Ok<BattleTokenResponse>>
    > CreateBattleToken(int seasonId, string opponentAvatarAddress)
    {
        var avatarAddress = HttpContext.User.RequireAvatarAddress();
        var defenderAvatarAddress = new Address(opponentAvatarAddress);

        var battleLog = await _battleLogRepo.AddBattleLogAsync(
            seasonId,
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
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(UnauthorizedHttpResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public Results<UnauthorizedHttpResult, NotFound<string>, Ok> RequestBattle(
        string txId,
        int logId,
        int seasonId
    )
    {
        _jobClient.Enqueue<BattleTaskProcessor>(processor => processor.ProcessAsync(txId, logId));

        return TypedResults.Ok();
    }

    [HttpGet("{battleLogId}")]
    [Authorize(Roles = "User", AuthenticationSchemes = "ES256K")]
    [ProducesResponseType(typeof(BattleLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(UnauthorizedHttpResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<
        Results<UnauthorizedHttpResult, NotFound<string>, Ok<BattleLogResponse>>
    > GetBattleLog(int battleLogId)
    {
        var battleLog = await _battleLogRepo.GetBattleLogAsync(battleLogId);

        if (battleLog is null)
        {
            return TypedResults.NotFound("Not battleLog.");
        }

        return TypedResults.Ok(battleLog.ToResponse());
    }
}
