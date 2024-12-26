namespace ArenaService.Services;

using ArenaService.Dtos;
using ArenaService.Extensions;
using ArenaService.Models;
using ArenaService.Repositories;

public class AvailableOpponentService
{
    private readonly IAvailableOpponentRepository _availableOpponentRepository;

    public AvailableOpponentService(IAvailableOpponentRepository availableOpponentRepository)
    {
        _availableOpponentRepository = availableOpponentRepository;
    }

    public async Task<List<Participant>> GetAvailableOpponents(int participantId)
    {
        var availableOpponents = await _availableOpponentRepository.GetAvailableOpponents(
            participantId
        );
        var opponents = availableOpponents.Select(ao => ao.Opponent).ToList();
        return opponents;
    }
}
