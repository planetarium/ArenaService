using Libplanet.Crypto;

namespace ArenaService.Repositories;

public interface IRankingService
{
    Task UpdateScoreAsync(
        Address avatarAddress,
        int seasonId,
        int roundId,
        int scoreChange,
        int? clanId
    );
}

public class RankingService : IRankingService
{
    private readonly IRankingRepository _rankingRepo;
    private readonly IClanRankingRepository _clanRankingRepo;

    public RankingService(IRankingRepository rankingRepo, IClanRankingRepository clanRankingRepo)
    {
        _rankingRepo = rankingRepo;
        _clanRankingRepo = clanRankingRepo;
    }

    public async Task UpdateScoreAsync(
        Address avatarAddress,
        int seasonId,
        int roundId,
        int scoreChange,
        int? clanId = null
    )
    {
        await _rankingRepo.UpdateScoreAsync(avatarAddress, seasonId, roundId, scoreChange);

        if (clanId is not null)
        {
            await _clanRankingRepo.UpdateScoreAsync(clanId.Value, seasonId, roundId, scoreChange);
        }
    }
}
