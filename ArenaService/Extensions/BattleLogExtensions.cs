namespace ArenaService.Extensions;

using ArenaService.Constants;
using ArenaService.Dtos;
using ArenaService.Models;

public static class BattleExtensions
{
    public static BattleResponse ToResponse(this Battle battle)
    {
        return new BattleResponse
        {
            Id = battle.Id,
            SeasonId = battle.AvailableOpponent.SeasonId,
            AttackerAvatarAddress = battle.AvailableOpponent.AvatarAddress,
            DefenderAvatarAddress = battle.AvailableOpponent.OpponentAvatarAddress,
            TxId = battle.TxId,
            TxStatus = battle.TxStatus,
            IsVictory = battle.IsVictory,
            ParticipantScore = battle.AvailableOpponent.Me.Score,
            ParticipantScoreChange = battle.MyScoreChange,
            OpponentScoreChange = battle.OpponentScoreChange,
        };
    }
}
