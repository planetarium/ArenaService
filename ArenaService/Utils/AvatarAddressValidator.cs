using System.Globalization;
using ArenaService.Shared.Extensions;
using Bencodex.Types;
using Libplanet.Crypto;

namespace ArenaService.Utils;

public class AvatarAddressValidator
{
    public static bool CheckSignerContainsAvatar(Address signer, Address avatarAddress)
    {
        const string deriveFormat = "avatar-state-{0}";
        const int slotCount = 3;

        var a = GetAvatarAddress(signer, 0);
        var b = GetAvatarAddress(signer, 1);
        var c = GetAvatarAddress(signer, 2);

        return Enumerable
            .Range(0, 3)
            .Select(index => GetAvatarAddress(signer, index))
            .Contains(avatarAddress);

        Address GetAvatarAddress(Address agentAddress, int index)
        {
            if (index < 0 || index >= slotCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    $"Index must be between 0 and 2."
                );
            }

            var deriveKey = string.Format(CultureInfo.InvariantCulture, deriveFormat, index);
            return agentAddress.Derive(deriveKey);
        }
    }
}
