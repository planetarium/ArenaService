namespace ArenaService.Worker.Rpc;

using System.Collections.Concurrent;
using ArenaService.Worker.Options;
using Bencodex;
using Bencodex.Types;
using Grpc.Core;
using Grpc.Net.Client;
using Lib9c.Renderers;
using Libplanet.Action;
using Libplanet.Crypto;
using Libplanet.Types.Blocks;
using MagicOnion.Client;
using Microsoft.Extensions.Options;
using Nekoyume.Action;
using Nekoyume.Shared.Hubs;
using Nekoyume.Shared.Services;

public class RpcClient
{
    private const int MaxDegreeOfParallelism = 8;
    private readonly Codec _codec = new();
    private readonly Receiver _receiver;
    private readonly Address _address;
    private readonly GrpcChannel _channel;
    private readonly ILogger<RpcClient> _logger;
    public IBlockChainService Service = null!;
    private IActionEvaluationHub _hub;

    private bool _ready;
    public bool Ready => _ready;
    private bool _selfDisconnect;

    public Block Tip => _receiver.Tip;
    public Block PreviousTip => _receiver.PreviousTip;


    public RpcClient(
        IOptions<RpcConfigOptions> options,
        ILogger<RpcClient> logger,
        Receiver receiver
    )
    {
        _logger = logger;
        _address = new PrivateKey().Address;
        var rpcConfigOptions = options.Value;
        _channel = GrpcChannel.ForAddress(
            $"http://{rpcConfigOptions.Host}:{rpcConfigOptions.Port}",
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

    public async Task<Dictionary<Address, IValue>> GetStates(
        byte[] hashBytes,
        byte[] accountBytes,
        List<byte[]> addressList
    )
    {
        var result = new ConcurrentDictionary<Address, IValue>();
        var queryResult = await Service.GetBulkStateByStateRootHash(
            hashBytes,
            accountBytes,
            addressList
        );
        queryResult
            .AsParallel()
            .WithDegreeOfParallelism(MaxDegreeOfParallelism)
            .ForAll(kv =>
            {
                result.TryAdd(new Address(kv.Key), _codec.Decode(kv.Value));
            });
        return result.ToDictionary(kv => kv.Key, kv => kv.Value);
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
}
