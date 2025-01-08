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
    private readonly IRankingRepository _rankingRepository;

    public LeaderboardController(
        ILeaderboardRepository leaderboardRepo,
        IParticipantRepository participantRepo,
        IRankingRepository rankingRepository
    )
    {
        _leaderboardRepo = leaderboardRepo;
        _participantRepo = participantRepo;
        _rankingRepository = rankingRepository;
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

        var rankingKey = $"ranking:season:{seasonId}";

        var rank = await _rankingRepository.GetRankAsync(rankingKey, participant.Id);
        var score = await _rankingRepository.GetScoreAsync(rankingKey, participant.Id);

        if (rank is null || score is null)
        {
            return TypedResults.NotFound("Not participant user.");
        }

        return TypedResults.Ok(
            new LeaderboardEntryResponse
            {
                AvatarAddress = participant.AvatarAddress,
                NameWithHash = participant.NameWithHash,
                PortraitId = participant.PortraitId,
                Rank = rank.Value,
                TotalScore = score.Value,
            }
        );
    }
}
