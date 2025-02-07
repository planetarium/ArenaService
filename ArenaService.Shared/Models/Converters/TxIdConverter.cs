using Libplanet.Types.Tx;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ArenaService.Shared.Models.Converters;

public class TxIdConverter : ValueConverter<TxId, string>
{
    public TxIdConverter()
        : base(address => address.ToHex(), hex => TxId.FromHex(hex)) { }
}
