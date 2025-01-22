using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace ArenaService.ActionValues;

public class TransferAssetsActionValue
{
    public Address Sender { get; set; }
    public Address Recipient { get; set; }
    public FungibleAssetValue Amount { get; set; }
    public string? Memo { get; set; } = null;
}
