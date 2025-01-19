using ArenaService.Repositories;
using Libplanet.Crypto;

namespace ArenaService.Worker;

public interface ISpecifyOpponentsService
{
    Task<List<(Address AvatarAddress, int GroupId, int Rank)>> SpecifyOpponentsAsync(
        Address avatarAddress,
        int seasonId,
        int roundId
    );
}

public class SpecifyOpponentsService : ISpecifyOpponentsService
{
    private readonly ILogger<SpecifyOpponentsService> _logger;
    private readonly IRankingRepository _rankingRepository;

    public SpecifyOpponentsService(
        ILogger<SpecifyOpponentsService> logger,
        IRankingRepository rankingRepository
    )
    {
        _logger = logger;
        _rankingRepository = rankingRepository;
    }

    public async Task<List<(Address AvatarAddress, int GroupId, int Rank)>> SpecifyOpponentsAsync(
        Address avatarAddress,
        int seasonId,
        int roundId
    )
    {
        var myScore = await _rankingRepository.GetScoreAsync(avatarAddress, seasonId);
        var opponents = await _rankingRepository.GetRandomParticipantsTempAsync(
            avatarAddress,
            seasonId,
            myScore.Value,
            5
        );

        return opponents.Select(o => (o.AvatarAddress, 1, o.Rank)).ToList();
    }
}
