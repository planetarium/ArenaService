namespace ArenaService.Controllers;

using System.Security.Claims;
using ArenaService.Dtos;
using ArenaService.Extensions;
using ArenaService.Repositories;
using ArenaService.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

[Route("seasons/{seasonId}/opponent/{opponentId}/battle")]
[ApiController]
public class BattleController : ControllerBase
{
    private readonly AvailableOpponentService _availableOpponentService;
    private readonly ParticipantService _participantService;
    private readonly IBattleLogRepository _battleLogRepository;

    public BattleController(
        AvailableOpponentService availableOpponentService,
        ParticipantService participantService,
        IBattleLogRepository battleLogRepository
    )
    {
        _availableOpponentService = availableOpponentService;
        _participantService = participantService;
        _battleLogRepository = battleLogRepository;
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

    [HttpPost]
    public async Task<
        Results<UnauthorizedHttpResult, NotFound<string>, Ok<string>>
    > CreateBattleToken(int seasonId, int opponentId)
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
        var battleLog = await _battleLogRepository.AddBattleLogAsync(
            participant.Id,
            opponentId,
            seasonId,
            "token"
        );

        return TypedResults.Ok(battleLog.Token);
    }
}
