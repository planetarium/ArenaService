namespace ArenaService.Controllers;

using ArenaService.Dtos;
using ArenaService.Extensions;
using ArenaService.Repositories;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

[Route("seasons/{seasonId}/leaderboard")]
[ApiController]
public class LeaderboardController : ControllerBase
{
    private readonly ILeaderboardRepository _leaderboardRepo;
    private readonly IParticipantRepository _participantRepo;

    public LeaderboardController(
        ILeaderboardRepository leaderboardRepo,
        IParticipantRepository participantRepo
    )
    {
        _leaderboardRepo = leaderboardRepo;
        _participantRepo = participantRepo;
    }

    // [HttpGet]
    // public async Task<Results<NotFound<string>, Ok<AvailableOpponentsResponse>>> GetLeaderboard(
    //     int seasonId,
    //     int offset,
    //     int limit
    // )
    // {
    //     var leaderboard = await _leaderboardRepository.GetLeaderboard(seasonId, offset, limit);
    // }

    [HttpGet("participants/{avatarAddress}")]
    [ProducesResponseType(typeof(LeaderboardEntryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(UnauthorizedHttpResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(NotFound<string>), StatusCodes.Status404NotFound)]
    public async Task<Results<NotFound<string>, Ok<LeaderboardEntryResponse>>> GetMyRank(
        int seasonId,
        string avatarAddress
    )
    {
        var participant = await _participantRepo.GetParticipantByAvatarAddressAsync(
            seasonId,
            avatarAddress
        );

        if (participant is null)
        {
            return TypedResults.NotFound("Not participant user.");
        }

        var leaderboardEntry = await _leaderboardRepo.GetMyRankAsync(seasonId, participant.Id);

        if (leaderboardEntry == null)
        {
            return TypedResults.NotFound("No leaderboardEntry found.");
        }

        leaderboardEntry.Participant = participant;

        return TypedResults.Ok(leaderboardEntry.ToResponse());
    }
}
