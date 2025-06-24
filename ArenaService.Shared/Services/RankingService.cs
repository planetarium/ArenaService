using Libplanet.Crypto;

namespace ArenaService.Shared.Repositories;

public interface IRankingService
{
    Task UpdateScoreAsync(
        Address avatarAddress,
        int seasonId,
        int roundIndex,
        int scoreChange,
        int? clanId
    );

    Task UpdateAllClanRankingAsync(int seasonId, int roundIndex, int roundInterval);
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
        int roundIndex,
        int scoreChange,
        int? clanId = null
    )
    {
        await _rankingRepo.UpdateScoreAsync(avatarAddress, seasonId, roundIndex, scoreChange);

        if (clanId is not null)
        {
            await _clanRankingRepo.UpdateScoreAsync(
                clanId.Value,
                avatarAddress,
                seasonId,
                roundIndex,
                scoreChange
            );
        }
    }

    public async Task UpdateAllClanRankingAsync(int seasonId, int roundIndex, int roundInterval)
    {
        var clans = await _clanRankingRepo.GetClansAsync(seasonId, roundIndex);

        var clanScores = new List<(int ClanId, int Score)>();

        foreach (var clanId in clans)
        {
            var topMembers = await _clanRankingRepo.GetTopClansAsync(clanId, seasonId, roundIndex);

            int totalScore = topMembers.Sum(member => member.Score);

            clanScores.Add((clanId, totalScore));
        }

        await _allClanRankingRepo.InitRankingAsync(clanScores, seasonId, roundIndex, roundInterval);
    }
}
