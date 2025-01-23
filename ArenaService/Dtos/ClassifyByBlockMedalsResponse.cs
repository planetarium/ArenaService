using ArenaService.Constants;
using Swashbuckle.AspNetCore.Annotations;

namespace ArenaService.Dtos;

public class ClassifyByBlockMedalsResponse
{
    [SwaggerSchema("(SeasonId, MedalCount)")]
    public Dictionary<int, int> Medals { get; set; }
    public int TotalMedalCountForThisChampionship { get; set; }
}
