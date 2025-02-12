using Libplanet.Crypto;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ArenaService.Models.Converters;

public class AddressConverter : ValueConverter<Address, string>
{
    public AddressConverter()
        : base(address => address.ToHex().ToLower(), hex => new Address(hex)) { }
}
