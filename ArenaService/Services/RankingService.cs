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
        int roundInterval,
        int? clanId
    );

    Task<List<(Address AvatarAddress, int GroupId, int Score)>> SpecifyOpponentsAsync(
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

    public async Task<List<(Address AvatarAddress, int GroupId, int Score)>> SpecifyOpponentsAsync(
        Address avatarAddress,
        int seasonId,
        int roundId
    )
    {
        var score = await _rankingRepo.GetScoreAsync(avatarAddress, seasonId, roundId);

        var opponents = await _groupRankingRepo.SelectBattleOpponentsAsync(
            avatarAddress,
            score,
            seasonId,
            roundId
        );

        var result = new List<(Address AvatarAddress, int GroupId, int Score)>();

        foreach (var opponent in opponents)
        {
            if (opponent.Value is null)
            {
                for (int i = opponent.Key; i <= 4; i++)
                {
                    var lowerGroupId = opponent.Key + i;
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
                            (lowerOpponent.Value.AvatarAddress, 3, lowerOpponent.Value.Score)
                        );
                        break;
                    }
                }
            }
            else
            {
                result.Add(
                    (opponent.Value.Value.AvatarAddress, opponent.Key, opponent.Value.Value.Score)
                );
            }
        }

        return result;
    }

    // 그룹 1~4
}
