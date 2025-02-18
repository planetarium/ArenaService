using Libplanet.Crypto;

namespace ArenaService.Shared.Repositories;

public interface IRankingService
{
    Task UpdateScoreAsync(
        Address avatarAddress,
        int seasonId,
        int roundId,
        int scoreChange,
        int? clanId
    );

    Task UpdateAllClanRankingAsync(int seasonId, int roundId, int roundInterval);
}

public class RankingService : IRankingService
{
    private readonly IRankingRepository _rankingRepo;
    private readonly IClanRankingRepository _clanRankingRepo;
    private readonly IAllClanRankingRepository _allClanRankingRepo;

    public RankingService(
        IRankingRepository rankingRepo,
        IClanRankingRepository clanRankingRepo,
        IAllClanRankingRepository allClanRankingRepo
    )
    {
        _rankingRepo = rankingRepo;
        _clanRankingRepo = clanRankingRepo;
        _allClanRankingRepo = allClanRankingRepo;
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
            await _clanRankingRepo.UpdateScoreAsync(
                clanId.Value,
                avatarAddress,
                seasonId,
                roundId,
                scoreChange
            );
        }
    }

    public async Task UpdateAllClanRankingAsync(int seasonId, int roundId, int roundInterval)
    {
        var clans = await _clanRankingRepo.GetClansAsync(seasonId, roundId);

        var clanScores = new List<(int ClanId, int Score)>();

        foreach (var clanId in clans)
        {
            var topMembers = await _clanRankingRepo.GetTopClansAsync(clanId, seasonId, roundId);

            int totalScore = topMembers.Sum(member => member.Score);

            clanScores.Add((clanId, totalScore));
        }

        await _allClanRankingRepo.InitRankingAsync(clanScores, seasonId, roundId, roundInterval);
    }
}
