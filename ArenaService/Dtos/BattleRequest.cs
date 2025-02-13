using ArenaService.Constants;
using ArenaService.Models;
using ArenaService.Models.Enums;
using Libplanet.Types.Tx;

namespace ArenaService.Dtos;

public class BattleRequest
{
    public required TxId TxId { get; set; }
}
