namespace ArenaService.Extensions;

using ArenaService.Constants;
using ArenaService.Dtos;
using ArenaService.Models;

public static class BattleExtensions
{
    public static BattleResponse ToResponse(this Battle battleLog)
    {
        return new BattleResponse
        {
            Id = battleLog.Id,
            SeasonId = battleLog.AvailableOpponent.SeasonId,
            AttackerAvatarAddress = battleLog.AvailableOpponent.AvatarAddress,
            DefenderAvatarAddress = battleLog.AvailableOpponent.OpponentAvatarAddress,
            TxId = battleLog.TxId,
            TxStatus = battleLog.TxStatus,
            IsVictory = battleLog.IsVictory,
            ParticipantScore = battleLog.AvailableOpponent.Me.Score,
            ParticipantScoreChange = battleLog.MyScoreChange,
            OpponentScoreChange = battleLog.OpponentScoreChange,
        };
    }
}
