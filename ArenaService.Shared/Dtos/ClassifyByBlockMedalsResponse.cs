using ArenaService.Shared.Constants;
using Swashbuckle.AspNetCore.Annotations;

namespace ArenaService.Shared.Dtos;

public class ClassifyByBlockMedalsResponse
{
    public required List<MedalResponse> Medals { get; set; }
    public required int TotalMedalCountForThisChampionship { get; set; }
}
