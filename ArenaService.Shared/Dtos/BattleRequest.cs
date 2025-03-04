using ArenaService.Shared.Constants;
using ArenaService.Shared.Models;
using ArenaService.Shared.Models.Enums;
using Libplanet.Types.Tx;

namespace ArenaService.Shared.Dtos;

public class BattleRequest
{
    public required TxId TxId { get; set; }
}
