using Libplanet.Crypto;

namespace ArenaService.Dtos;

public class SeasonsResponse
{
    public required List<SeasonResponse> Seasons { get; set; }

    public required Address OperationAccountAddress { get; set; }
}
