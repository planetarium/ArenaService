using ArenaService.Client;
using ArenaService.Constants;
using ArenaService.Repositories;
using ArenaService.Services;
using Libplanet.Crypto;
using Libplanet.Types.Tx;

namespace ArenaService.Worker;

public class RefreshProcessor
{
    private readonly ILogger<RefreshProcessor> _logger;
    private readonly IHeadlessClient _client;
    private readonly ISeasonRepository _seasonRepo;
    private readonly IRoundRepository _roundRepo;
    private readonly IAvailableOpponentRepository _availableOpponentRepository;
    private readonly IRankingRepository _rankingRepository;
    private readonly ITxTrackingService _txTrackingService;

    public RefreshProcessor(
        ILogger<RefreshProcessor> logger,
        IHeadlessClient client,
        ISeasonRepository seasonRepo,
        IRoundRepository roundRepo,
        IAvailableOpponentRepository availableOpponentRepository,
        ITxTrackingService txTrackingService,
        IRankingRepository rankingRepository
    )
    {
        _logger = logger;
        _client = client;
        _roundRepo = roundRepo;
        _availableOpponentRepository = availableOpponentRepository;
        _rankingRepository = rankingRepository;
        _txTrackingService = txTrackingService;
        _seasonRepo = seasonRepo;
    }

    public async Task ProcessAsync(Address avatarAddress, int seasonId, int roundId, TxId? txId)
    {
        _logger.LogInformation($"Calc ao: {avatarAddress}, {seasonId}, {roundId}");

        var refreshRequests = await _availableOpponentRepository.GetRefreshRequests(
            avatarAddress,
            roundId
        );
        var refreshCount = refreshRequests.Count;

        // var policy = OpponentRefreshCosts.GetPolicy(refreshCount);

        // if (policy.Source != UpdateSource.FREE)
        // {
        //     if (txId is null)
        //     {
        //         throw new ArgumentNullException("Subsequent refresh costs required");
        //     }

        //     await _txTrackingService.TrackTransactionAsync(
        //         txId.Value,
        //         async status =>
        //         {
        //             Console.WriteLine($"Status updated: {status}");
        //         },
        //         async successResponse =>
        //         {
        //             Console.WriteLine($"Transaction succeeded! Response: {successResponse}");
        //         },
        //         async transactionId =>
        //         {
        //             Console.WriteLine($"Transaction timed out for ID: {transactionId}");
        //         }
        //     );
        // }

        // var myScore = await _rankingRepository.GetScoreAsync(avatarAddress, seasonId);
        // var opponents = await _rankingRepository.GetRandomParticipantsTempAsync(
        //     avatarAddress,
        //     seasonId,
        //     myScore.Value,
        //     5
        // );

        // await _availableOpponentRepository.AddAvailableOpponents(
        //     avatarAddress,
        //     roundId,
        //     opponents.Select(o => o.AvatarAddress).ToList()
        // );
    }
}
