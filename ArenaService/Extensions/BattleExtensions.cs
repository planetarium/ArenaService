namespace ArenaService.Extensions;

using ArenaService.Dtos;
using ArenaService.Models;

public static class BattleExtensions
{
    public static BattleResponse ToResponse(this Battle battle, int score)
    {
        return new BattleResponse
        {
            Id = battle.Id,
            SeasonId = battle.SeasonId,
            MyAvatarAddress = battle.AvatarAddress,
            OpponentAvatarAddress = battle.AvailableOpponent.OpponentAvatarAddress,
            BattleStatus = battle.BattleStatus,
            TxId = battle.TxId,
            TxStatus = battle.TxStatus,
            IsVictory = battle.IsVictory,
            MyScore = score,
            MyScoreChange = battle.MyScoreChange,
            OpponentScoreChange = battle.OpponentScoreChange,
        };
    }
}
