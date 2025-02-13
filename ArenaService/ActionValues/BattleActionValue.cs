using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace ArenaService.ActionValues;

public class BattleActionValue
{
    public Address MyAvatarAddress { get; set; }
    public Address EnemyAvatarAddress { get; set; }
    public FungibleAssetValue Amount { get; set; }
    public string Memo { get; set; }
    public string ArenaProvider { get; set; }
    public bool ChargeAp { get; set; }
    public List<Guid> Costumes { get; set; }
    public List<Guid> Equipments { get; set; }
}
