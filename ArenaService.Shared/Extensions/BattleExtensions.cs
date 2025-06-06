namespace ArenaService.Shared.Extensions;

using ArenaService.Shared.Dtos;
using ArenaService.Shared.Models;

public static class BattleExtensions
{
    public static BattleResponse ToResponse(this Battle battle)
    {
        return new BattleResponse
        {
            Id = battle.Id,
            SeasonId = battle.SeasonId,
            MyAvatarAddress = battle.AvatarAddress,
            OpponentAvatarAddress = battle.AvailableOpponent.OpponentAvatarAddress,
            BattleStatus = battle.BattleStatus,
            TxId = battle.TxId,
            TxStatus = battle.TxStatus is null ? Models.Enums.TxStatus.INVALID : battle.TxStatus,
            IsVictory = battle.IsVictory,
            MyScore = battle.Participant.Score,
            MyScoreChange = battle.MyScoreChange,
            OpponentScoreChange = battle.OpponentScoreChange,
        };
    }
}
