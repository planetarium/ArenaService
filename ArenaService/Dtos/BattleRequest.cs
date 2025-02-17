using ArenaService.Constants;
using ArenaService.Shared.Models;
using ArenaService.Shared.Models.Enums;
using Libplanet.Types.Tx;

namespace ArenaService.Dtos;

public class BattleRequest
{
    public required TxId TxId { get; set; }
}
