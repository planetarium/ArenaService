using Libplanet.Crypto;

namespace ArenaService.Repositories;

public interface IRankingService
{
    Task UpdateScoreAsync(
        Address avatarAddress,
        int seasonId,
        int roundId,
        int prevScore,
        int scoreChange,
        int? clanId
    );

    Task<List<(Address AvatarAddress, int GroupId, int Score, int Rank)>> SpecifyOpponentsAsync(
        Address avatarAddress,
        int seasonId,
        int roundId
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
        int? clanId = null
    )
    {
        await _rankingRepo.UpdateScoreAsync(avatarAddress, seasonId, roundId, scoreChange);
        await _groupRankingRepo.UpdateScoreAsync(
            avatarAddress,
            seasonId,
            roundId,
            prevScore,
            prevScore + scoreChange
        );

        if (clanId is not null)
        {
            await _clanRankingRepo.UpdateScoreAsync(clanId.Value, seasonId, roundId, scoreChange);
        }
    }

    public async Task<
        List<(Address AvatarAddress, int GroupId, int Score, int Rank)>
    > SpecifyOpponentsAsync(Address avatarAddress, int seasonId, int roundId)
    {
        var score = await _rankingRepo.GetScoreAsync(avatarAddress, seasonId, roundId);

        var opponents = await _groupRankingRepo.SelectBattleOpponentsAsync(
            avatarAddress,
            score,
            seasonId,
            roundId
        );

        var result = new List<(Address AvatarAddress, int GroupId, int Score, int Rank)>();

        foreach (var opponent in opponents)
        {
            if (opponent.Value is null)
            {
                var lowerGroupId = opponent.Key + 1;
                var lowerGroupOpponents = await _groupRankingRepo.SelectBattleOpponentsAsync(
                    avatarAddress,
                    score,
                    seasonId,
                    roundId
                );

                if (
                    lowerGroupOpponents.TryGetValue(lowerGroupId, out var lowerOpponent)
                    && lowerOpponent.HasValue
                )
                {
                    result.Add(
                        (
                            lowerOpponent.Value.AvatarAddress,
                            3,
                            lowerOpponent.Value.Score,
                            lowerGroupId
                        )
                    );
                }
                else
                {
                    result.Add((default, opponent.Key, 0, opponent.Key));
                }
            }
            else
            {
                result.Add(
                    (
                        opponent.Value.Value.AvatarAddress,
                        opponent.Key,
                        opponent.Value.Value.Score,
                        opponent.Key
                    )
                );
            }
        }

        return result;
    }
}
