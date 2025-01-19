using ArenaService.Extensions;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace ArenaService
{
    public class TransferAssetsParser
    {
        public static (
            Address Sender,
            Address Recipient,
            FungibleAssetValue Amount,
            string? Memo
        ) ParseActionPayload(Dictionary plainValue)
        {
            var sender = plainValue["sender"].ToAddress();
            var recipient = plainValue["recipient"].ToAddress();
            var amount = plainValue["amount"].ToFungibleAssetValue();
            var memo = plainValue.TryGetValue((Text)"memo", out var memoValue)
                ? memoValue.ToDotnetString()
                : null;

            return (sender, recipient, amount, memo);
        }
    }
}
