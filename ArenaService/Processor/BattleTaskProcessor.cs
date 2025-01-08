namespace ArenaService.Worker;

public class BattleTaskProcessor
{
    private readonly ILogger<BattleTaskProcessor> _logger;

    public BattleTaskProcessor(ILogger<BattleTaskProcessor> logger)
    {
        _logger = logger;
    }

    public async Task ProcessAsync(string taskId)
    {
        _logger.LogInformation($"Starting long-running task for TaskId: {taskId}");

        // 예: 시간이 오래 걸리는 작업 (3초 대기)
        await Task.Delay(300000);

        _logger.LogInformation($"Completed long-running task for TaskId: {taskId}");
    }
}
