using ArenaService.Shared.Constants;
using ArenaService.Shared.Models;
using ArenaService.Shared.Models.Enums;
using Libplanet.Crypto;
using Libplanet.Types.Tx;

namespace ArenaService.Dtos;

public class BattleResponse
{
    public required int Id { get; set; }
    public required int SeasonId { get; set; }

    public required Address MyAvatarAddress { get; set; }

    public required Address OpponentAvatarAddress { get; set; }

    public required BattleStatus BattleStatus { get; set; }
    public TxId? TxId { get; set; }
    public TxStatus? TxStatus { get; set; }
    public bool? IsVictory { get; set; }
    public int? MyScore { get; set; }
    public int? MyScoreChange { get; set; }
    public int? OpponentScoreChange { get; set; }
}
