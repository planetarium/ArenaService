using ArenaService.ActionValues;
using ArenaService.Extensions;
using Bencodex.Types;

namespace ArenaService
{
    public class TransferAssetsParser
    {
        public static TransferAssetsActionValue ParseActionPayload(Dictionary plainValue)
        {
            var sender = plainValue["sender"].ToAddress();
            var recipient = plainValue["recipient"].ToAddress();
            var amount = plainValue["amount"].ToFungibleAssetValue();
            var memo = plainValue.TryGetValue((Text)"memo", out var memoValue)
                ? memoValue.ToDotnetString()
                : null;

            return new TransferAssetsActionValue
            {
                Sender = sender,
                Recipient = recipient,
                Amount = amount,
                Memo = memo
            };
        }
    }
}
