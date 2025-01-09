using ArenaService.Client;
using ArenaService.Repositories;
using Bencodex;
using Bencodex.Types;
using Libplanet.Crypto;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Action.Arena;

namespace ArenaService.Worker;

public class FakeBattleTaskProcessor
{
    private static readonly Codec Codec = new();
    private readonly ILogger<BattleTaskProcessor> _logger;
    private readonly IHeadlessClient _client;
    private readonly IBattleLogRepository _battleLogRepo;
    private readonly IParticipantRepository _participantRepo;

    public FakeBattleTaskProcessor(
        ILogger<BattleTaskProcessor> logger,
        IHeadlessClient client,
        IBattleLogRepository battleLogRepo,
        IParticipantRepository participantRepo
    )
    {
        _logger = logger;
        _client = client;
        _battleLogRepo = battleLogRepo;
        _participantRepo = participantRepo;
    }

    public async Task ProcessAsync(string txId, int battleLogId)
    {
        _logger.LogInformation($"Watch the battle: {txId}, {battleLogId}");

        var battleLog = await _battleLogRepo.GetBattleLogAsync(battleLogId);
        await Task.Delay(8000);

        await _battleLogRepo.UpdateBattleResultAsync(battleLogId, true, 10, 10, 1);
        await _participantRepo.UpdateScoreAsync(
            battleLog.SeasonId,
            new Address(battleLog.Attacker.AvatarAddress),
            10
        );
    }
}
