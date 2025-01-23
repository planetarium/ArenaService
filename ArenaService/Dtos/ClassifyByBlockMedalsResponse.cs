using ArenaService.Constants;
using Swashbuckle.AspNetCore.Annotations;

namespace ArenaService.Dtos;

public class ClassifyByBlockMedalsResponse
{
    [SwaggerSchema("(SeasonId, MedalCount)")]
    public required Dictionary<int, int> Medals { get; set; }
    public required int TotalMedalCountForThisChampionship { get; set; }
}
