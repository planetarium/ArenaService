using ArenaService.Constants;
using ArenaService.Models;
using ArenaService.Models.Enums;
using Libplanet.Types.Tx;

namespace ArenaService.Dtos;

public class BattleRequest
{
    public TxId TxId { get; set; }
}
