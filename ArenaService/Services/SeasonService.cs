namespace ArenaService.Services;

using ArenaService.Dtos;
using ArenaService.Extensions;
using ArenaService.Repositories;

public class SeasonService
{
    private readonly ISeasonRepository _seasonRepository;

    public SeasonService(ISeasonRepository seasonRepository)
    {
        _seasonRepository = seasonRepository;
    }

    public async Task<SeasonResponse?> GetCurrentSeasonAsync(int blockIndex)
    {
        var seasons = await _seasonRepository.GetActivatedSeasonsAsync();
        var currentSeason = seasons.FirstOrDefault(s =>
            s.StartBlockIndex <= blockIndex && s.EndBlockIndex >= blockIndex
        );

        return currentSeason?.ToResponse();
    }
}
