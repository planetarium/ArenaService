using ArenaService.Client;
using ArenaService.Repositories;
using Libplanet.Crypto;

namespace ArenaService.Worker;

public class CalcAvailableOpponentsProcessor
{
    private readonly ILogger<CalcAvailableOpponentsProcessor> _logger;
    private readonly IHeadlessClient _client;
    private readonly ISeasonRepository _seasonRepo;
    private readonly IArenaIntervalRepository _arenaIntervalRepo;
    private readonly IAvailableOpponentRepository _availableOpponentRepository;
    private readonly IRankingRepository _rankingRepository;

    public CalcAvailableOpponentsProcessor(
        ILogger<CalcAvailableOpponentsProcessor> logger,
        IHeadlessClient client,
        ISeasonRepository seasonRepo,
        IArenaIntervalRepository arenaIntervalRepo,
        IAvailableOpponentRepository availableOpponentRepository,
        IRankingRepository rankingRepository
    )
    {
        _logger = logger;
        _client = client;
        _arenaIntervalRepo = arenaIntervalRepo;
        _availableOpponentRepository = availableOpponentRepository;
        _rankingRepository = rankingRepository;
        _seasonRepo = seasonRepo;
    }

    public async Task ProcessAsync(Address participantAvatarAddress, int seasonId)
    {
        _logger.LogInformation($"Calc ao: {participantAvatarAddress}, {seasonId}");

        var tipResponse = await _client.GetTipIndex.ExecuteAsync();

        if (tipResponse.Data is null)
        {
            _logger.LogInformation($"tipResponse is null");
            return;
        }

        var blockIndex = tipResponse.Data.NodeStatus.Tip.Index;

        var season = await _seasonRepo.GetSeasonAsync(seasonId);

        if (season == null)
        {
            _logger.LogInformation($"season is null");
            return;
        }

        var currentArenaInterval = season.ArenaIntervals.FirstOrDefault(ai =>
            ai.StartBlock <= blockIndex && ai.EndBlock >= blockIndex
        );

        if (currentArenaInterval == null)
        {
            _logger.LogInformation($"currentArenaInterval is null");
            return;
        }

        var rankingKey = $"ranking:season:{seasonId}";
        var myScore = await _rankingRepository.GetScoreAsync(
            rankingKey,
            participantAvatarAddress.ToHex(),
            seasonId
        );
        var opponents = await _rankingRepository.GetRandomParticipantsTempAsync(
            rankingKey,
            participantAvatarAddress.ToHex(),
            seasonId,
            myScore.Value,
            5
        );

        await _availableOpponentRepository.AddAvailableOpponents(
            participantAvatarAddress,
            seasonId,
            currentArenaInterval.Id,
            opponents.Select(o => o.ParticipantAvatarAddress).ToList()
        );
    }
}
