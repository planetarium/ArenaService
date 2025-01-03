namespace ArenaService.Worker;

using System.Collections.Concurrent;
using System.Diagnostics;
using Bencodex;
using Bencodex.Types;
using Grpc.Core;
using Grpc.Net.Client;
using Lib9c.Model.Order;
using Lib9c.Renderers;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Blocks;
using MagicOnion.Client;
using Microsoft.Extensions.Options;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.Market;
using Nekoyume.Model.State;
using Nekoyume.Shared.Hubs;
using Nekoyume.Shared.Services;
using Nekoyume.TableData;
using Nekoyume.TableData.Crystal;

public class RpcClient
{
    private readonly Address _address;
    private readonly GrpcChannel _channel;
    private readonly Codec _codec = new();
    private readonly ILogger<RpcClient> _logger;
    private readonly Receiver _receiver;
    private bool _ready;
    private bool _selfDisconnect;

    public IBlockChainService Service = null!;
    private IActionEvaluationHub _hub;

    public bool Ready => _ready;
    public Block Tip => _receiver.Tip;
    public Block PreviousTip => _receiver.PreviousTip;

    private readonly ActionRenderer _actionRenderer;

    public RpcClient(ILogger<RpcClient> logger, Receiver receiver, ActionRenderer actionRenderer)
    {
        _logger = logger;
        _address = new PrivateKey().Address;
        _channel = GrpcChannel.ForAddress(
            "http://odin-rpc-1.nine-chronicles.com:31238",
            new GrpcChannelOptions
            {
                Credentials = ChannelCredentials.Insecure,
                MaxReceiveMessageSize = null,
                HttpHandler = new SocketsHttpHandler
                {
                    EnableMultipleHttp2Connections = true,
                    KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                }
            }
        );
        _receiver = receiver;
        _actionRenderer = actionRenderer;
        _actionRenderer.ActionRenderSubject.Subscribe(RenderAction);
    }

    /// <summary>
    /// Insert or Update <see cref="ProductModel"/> by Market related actions.
    /// </summary>
    /// <param name="ev"></param>
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

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                _selfDisconnect = true;
                stoppingToken.ThrowIfCancellationRequested();
            }

            try
            {
                await Join(stoppingToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error occurred");
                _ready = false;
            }
            if (_selfDisconnect)
            {
                _logger.LogInformation("self disconnect");
                break;
            }
        }
    }

    private async Task Join(CancellationToken stoppingToken)
    {
        _hub = await StreamingHubClient.ConnectAsync<
            IActionEvaluationHub,
            IActionEvaluationHubReceiver
        >(_channel, _receiver, cancellationToken: stoppingToken);
        _logger.LogDebug("Connected to hub");
        Service = MagicOnionClient
            .Create<IBlockChainService>(_channel)
            .WithCancellationToken(stoppingToken);
        _logger.LogDebug("Connected to service");

        await _hub.JoinAsync(_address.ToHex());
        await Service.AddClient(_address.ToByteArray());
        _logger.LogInformation("Joined to RPC headless");
        _ready = true;

        _logger.LogDebug("Waiting for disconnecting");
        await _hub.WaitForDisconnect();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _selfDisconnect = true;
        await _hub.LeaveAsync();
    }

    public async Task<byte[]> GetBlockStateRootHashBytes()
    {
        while (Tip is null)
        {
            await Task.Delay(1000);
        }
        return _receiver.Tip.StateRootHash.ToByteArray();
    }

    internal class LocalRandom : Random, IRandom
    {
        public int Seed { get; }

        public LocalRandom(int seed)
            : base(seed)
        {
            Seed = seed;
        }
    }
}
