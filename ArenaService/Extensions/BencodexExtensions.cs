using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace ArenaService.Extensions;

public static class ValueExtensions
{
    public static Address ToAddress(this IValue serialized) => new Address(serialized);

    public static string ToDotnetString(this IValue serialized) => ((Text)serialized).Value;

    public static FungibleAssetValue ToFungibleAssetValue(this IValue serialized) =>
        serialized is Bencodex.Types.List serializedList
            ? new FungibleAssetValue(serializedList)
            : throw new InvalidCastException();
}
