namespace ArenaService.Extensions;

using ArenaService.Constants;
using ArenaService.Dtos;
using ArenaService.Models;

public static class BattleLogExtensions
{
    public static BattleLogResponse ToResponse(this BattleLog battleLog)
    {
        return new BattleLogResponse
        {
            Id = battleLog.Id,
            SeasonId = battleLog.SeasonId,
            AttackerAvatarAddress = battleLog.AttackerAvatarAddress,
            DefenderAvatarAddress = battleLog.DefenderAvatarAddress,
            TxId = battleLog.TxId,
            BattleTxTrackingStatus = battleLog.TxStatus is null
                ? BattleTxTrackingStatus.PENDING
                : battleLog.IsVictory is null
                    ? BattleTxTrackingStatus.TRACKING
                    : BattleTxTrackingStatus.COMPLETED,
            TxStatus = battleLog.TxStatus,
            IsVictory = battleLog.IsVictory,
            ParticipantScore = battleLog.Attacker.Score,
            ParticipantScoreChange = battleLog.ParticipantScoreChange,
            OpponentScoreChange = battleLog.OpponentScoreChange,
            BattleBlockIndex = battleLog.BattleBlockIndex,
        };
    }
}
