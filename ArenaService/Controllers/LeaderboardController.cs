namespace ArenaService.Controllers;

using ArenaService.Dtos;
using ArenaService.Extensions;
using ArenaService.Repositories;
using Libplanet.Crypto;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

[Route("seasons/{seasonId}/leaderboard")]
[ApiController]
public class LeaderboardController : ControllerBase
{
    private readonly IParticipantRepository _participantRepo;
    private readonly IRankingRepository _rankingRepository;

    public LeaderboardController(
        IParticipantRepository participantRepo,
        IRankingRepository rankingRepository
    )
    {
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
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    public async Task<Results<NotFound<string>, Ok<LeaderboardEntryResponse>>> GetMyRank(
        int seasonId,
        string avatarAddress
    )
    {
        var participant = await _participantRepo.GetParticipantAsync(
            seasonId,
            new Address(avatarAddress)
        );

        if (participant is null)
        {
            return TypedResults.NotFound("Not participant user.");
        }

        var rankingKey = $"ranking:season:{seasonId}";

        var rank = await _rankingRepository.GetRankAsync(
            rankingKey,
            participant.AvatarAddress,
            seasonId
        );
        var score = await _rankingRepository.GetScoreAsync(
            rankingKey,
            participant.AvatarAddress,
            seasonId
        );

        if (rank is null || score is null)
        {
            return TypedResults.NotFound("rank null.");
        }

        var participantResponse = participant.ToResponse();

        return TypedResults.Ok(
            new LeaderboardEntryResponse
            {
                AvatarAddress = participantResponse.AvatarAddress,
                NameWithHash = participantResponse.NameWithHash,
                PortraitId = participantResponse.PortraitId,
                Cp = participantResponse.Cp,
                Level = participantResponse.Level,
                SeasonId = participantResponse.SeasonId,
                Score = participantResponse.Score,
                Rank = rank.Value,
            }
        );
    }
}
