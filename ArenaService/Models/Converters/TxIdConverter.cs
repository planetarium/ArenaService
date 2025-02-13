using Libplanet.Types.Tx;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ArenaService.Models.Converters;

public class TxIdConverter : ValueConverter<TxId, string>
{
    public TxIdConverter()
        : base(address => address.ToHex().ToLower(), hex => TxId.FromHex(hex)) { }
}
