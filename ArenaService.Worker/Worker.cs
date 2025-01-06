namespace ArenaService.Worker;

using ArenaService.Worker.Rpc;
using ArenaService.Worker.Utils;
using Lib9c.Renderers;
using Libplanet.Action;
using Nekoyume.Action;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ActionRenderer _actionRenderer;

    public Worker(ILogger<Worker> logger, ActionRenderer actionRenderer)
    {
        _logger = logger;

        _actionRenderer = actionRenderer;
        _actionRenderer.ActionRenderSubject.Subscribe(RenderAction);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            _logger.LogInformation("worker");
            await Task.Delay(100000, stoppingToken);
        }
    }

    public async void RenderAction(ActionEvaluation<ActionBase> ev)
    {
        if (ev.Exception is null)
        {
            var seed = ev.RandomSeed;
            var random = new LocalRandom(seed);
            var stateRootHash = ev.OutputState;
            var hashBytes = stateRootHash.ToByteArray();
            switch (ev.Action)
            {
                // Insert new product
                case DailyReward d:
                {
                    break;
                }
            }
        }
    }
}
