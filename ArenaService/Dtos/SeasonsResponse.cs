using ArenaService.Constants;
using Libplanet.Crypto;
using Swashbuckle.AspNetCore.Annotations;

namespace ArenaService.Dtos;

public class SeasonsResponse
{
    public required List<SeasonResponse> Seasons { get; set; }

    public Address OperationAccountAddress { get; set; }
}
