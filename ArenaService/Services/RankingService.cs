using Libplanet.Crypto;

namespace ArenaService.Shared.Repositories;

public interface IRankingService
{
    Task UpdateScoreAsync(
        Address avatarAddress,
        int seasonId,
        int roundId,
        int prevScore,
        int scoreChange,
        int roundInterval,
        int? clanId
    );
}

public class RankingService : IRankingService
{
    private readonly IRankingRepository _rankingRepo;
    private readonly IGroupRankingRepository _groupRankingRepo;
    private readonly IClanRankingRepository _clanRankingRepo;

    public RankingService(
        IRankingRepository rankingRepo,
        IClanRankingRepository clanRankingRepo,
        IGroupRankingRepository groupRankingRepo
    )
    {
        _rankingRepo = rankingRepo;
        _clanRankingRepo = clanRankingRepo;
        _groupRankingRepo = groupRankingRepo;
    }

    public async Task UpdateScoreAsync(
        Address avatarAddress,
        int seasonId,
        int roundId,
        int prevScore,
        int scoreChange,
        int roundInterval,
        int? clanId = null
    )
    {
        await _rankingRepo.UpdateScoreAsync(avatarAddress, seasonId, roundId, scoreChange);
        await _groupRankingRepo.UpdateScoreAsync(
            avatarAddress,
            seasonId,
            roundId,
            prevScore,
            prevScore + scoreChange,
            roundInterval
        );

        if (clanId is not null)
        {
            await _clanRankingRepo.UpdateScoreAsync(clanId.Value, seasonId, roundId, scoreChange);
        }
    }
}
