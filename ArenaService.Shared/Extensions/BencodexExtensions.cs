using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace ArenaService.Shared.Extensions;

public static class ValueExtensions
{
    public static Address ToAddress(this IValue serialized) => new Address(serialized);

    public static string ToDotnetString(this IValue serialized) => ((Text)serialized).Value;

    public static FungibleAssetValue ToFungibleAssetValue(this IValue serialized) =>
        serialized is Bencodex.Types.List serializedList
            ? new FungibleAssetValue(serializedList)
            : throw new InvalidCastException();

    public static bool ToBoolean(this IValue serialized) =>
        ((Bencodex.Types.Boolean)serialized).Value;

    public static Guid ToGuid(this IValue serialized) =>
        new Guid(((Binary)serialized).ToByteArray());
}
