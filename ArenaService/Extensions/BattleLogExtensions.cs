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
            BattleStatus = battleLog.TxStatus is null
                ? BattleStatus.PENDING
                : battleLog.IsVictory is null
                    ? BattleStatus.TRACKING
                    : BattleStatus.COMPLETED,
            TxStatus = battleLog.TxStatus.ToString(),
            IsVictory = battleLog.IsVictory,
            ParticipantScore = battleLog.Attacker.Score,
            ParticipantScoreChange = battleLog.ParticipantScoreChange,
            OpponentScoreChange = battleLog.OpponentScoreChange,
            BattleBlockIndex = battleLog.BattleBlockIndex,
        };
    }
}
