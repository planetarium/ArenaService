using ArenaService.Constants;
using ArenaService.Models;
using ArenaService.Models.Enums;
using Libplanet.Crypto;
using Libplanet.Types.Tx;

namespace ArenaService.Dtos;

public class BattleResponse
{
    public int Id { get; set; }
    public int SeasonId { get; set; }

    public Address MyAvatarAddress { get; set; }

    public Address OpponentAvatarAddress { get; set; }

    public BattleStatus BattleStatus { get; set; }
    public TxId? TxId { get; set; }
    public TxStatus? TxStatus { get; set; }
    public bool? IsVictory { get; set; }
    public int? MyScore { get; set; }
    public int? MyScoreChange { get; set; }
    public int? OpponentScoreChange { get; set; }
}
