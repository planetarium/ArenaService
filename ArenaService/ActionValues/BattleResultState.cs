using System.Numerics;
using ArenaService.Shared.Extensions;
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Tx;

namespace ArenaService.ActionValues;

public class BattleResultState
{
    public static Address DeriveAddress(Address avatarAddress, TxId txId) =>
        avatarAddress.Derive(txId.ToString());

    public bool IsVictory;
    public int PortraitId;
    public int Level;
    public long Cp;

    public BattleResultState(IValue bencoded)
    {
        if (bencoded is not List l)
        {
            throw new ArgumentException(
                $"Invalid bencoded value: {bencoded.Inspect()}",
                nameof(bencoded)
            );
        }

        IsVictory = l[0].ToBoolean();
        PortraitId = (Integer)l[1];
        Level = (Integer)l[2];
        Cp = (Integer)l[3];
    }
}
