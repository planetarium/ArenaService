using Libplanet.Crypto;

namespace ArenaService.Options;

public class OpsAddressOptions
{
    public const string SectionName = "OpsAddress";

    public required Address address { get; init; }
}
