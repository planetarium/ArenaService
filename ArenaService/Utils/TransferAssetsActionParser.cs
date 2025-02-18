using System.Text.RegularExpressions;
using ArenaService.ActionValues;
using ArenaService.Shared.Extensions;
using Bencodex.Types;

namespace ArenaService.Utils;

public class TransferAssetsActionParser
{
    public static bool TryParseActionPayload(
        IValue plainValue,
        out TransferAssetsActionValue taActionValue
    )
    {
        taActionValue = null;

        var (actionType, actionValues) = ActionParser.DeconstructActionPlainValue(plainValue);

        var actionTypeStr = actionType switch
        {
            Integer integer => integer.ToString(),
            Text text => (string)text,
            _ => null
        };

        if (actionTypeStr is null || actionValues is null)
        {
            return false;
        }

        if (Regex.IsMatch(actionTypeStr, "^transfer_asset[0-9]*$"))
        {
            taActionValue = ParseActionPayload((Dictionary)actionValues);
            return true;
        }

        return false;
    }

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
