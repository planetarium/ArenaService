using ArenaService.Constants;
using Swashbuckle.AspNetCore.Annotations;

namespace ArenaService.Dtos;

public class SeasonsResponse
{
    public required List<SeasonResponse> Seasons { get; set; }

    public required string OperationAccountAddress { get; set; }
}
