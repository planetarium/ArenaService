using System.Text;
using ArenaService.Client;
using ArenaService.Shared.Jwt;
using ArenaService.Shared.Models.Enums;
using ArenaService.Shared.Repositories;
using ArenaService.Utils;
using ArenaService.Worker;
using Bencodex;
using Bencodex.Types;
using Hangfire;
using Libplanet.Crypto;
using Libplanet.Types.Tx;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using System.Reflection;

namespace ArenaService.Tests;

public class BattleTxTrackerTests
{
    private Mock<ILogger<BattleTxTracker>> _loggerMock;
    private Mock<IServiceProvider> _serviceProviderMock;
    private Mock<IHeadlessClient> _headlessClientMock;
    private Mock<IConnectionMultiplexer> _redisMock;
    private Mock<IDatabase> _databaseMock;
    private Mock<ISeasonCacheRepository> _seasonCacheRepoMock;
    private Mock<IBackgroundJobClient> _jobClientMock;
    private Mock<IBattleRepository> _battleRepoMock;
    private Mock<BattleTokenValidator> _battleTokenValidatorMock;
    private Mock<IBlockTrackerRepository> _blockTrackerRepoMock;
    private BattleTxTracker _battleTxTracker;
    private CancellationTokenSource _cancellationTokenSource;

    public BattleTxTrackerTests()
    {
        _loggerMock = new Mock<ILogger<BattleTxTracker>>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _headlessClientMock = new Mock<IHeadlessClient>();
        _redisMock = new Mock<IConnectionMultiplexer>();
        _databaseMock = new Mock<IDatabase>();
        _seasonCacheRepoMock = new Mock<ISeasonCacheRepository>();
        _jobClientMock = new Mock<IBackgroundJobClient>();
        _battleRepoMock = new Mock<IBattleRepository>();
        _battleTokenValidatorMock = new Mock<BattleTokenValidator>();
        _blockTrackerRepoMock = new Mock<IBlockTrackerRepository>();
        _cancellationTokenSource = new CancellationTokenSource();

        var serviceScopeMock = new Mock<IServiceScope>();
        var serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();

        _redisMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_databaseMock.Object);
        
        serviceScopeMock.Setup(x => x.ServiceProvider).Returns(_serviceProviderMock.Object);
        serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(serviceScopeMock.Object);
        
        _serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory))).Returns(serviceScopeFactoryMock.Object);
        _serviceProviderMock.Setup(x => x.GetService(typeof(IHeadlessClient))).Returns(_headlessClientMock.Object);
        _serviceProviderMock.Setup(x => x.GetService(typeof(IConnectionMultiplexer))).Returns(_redisMock.Object);
        _serviceProviderMock.Setup(x => x.GetService(typeof(ISeasonCacheRepository))).Returns(_seasonCacheRepoMock.Object);
        _serviceProviderMock.Setup(x => x.GetService(typeof(IBackgroundJobClient))).Returns(_jobClientMock.Object);
        _serviceProviderMock.Setup(x => x.GetService(typeof(IBattleRepository))).Returns(_battleRepoMock.Object);
        _serviceProviderMock.Setup(x => x.GetService(typeof(BattleTokenValidator))).Returns(_battleTokenValidatorMock.Object);
        _serviceProviderMock.Setup(x => x.GetService(typeof(IBlockTrackerRepository))).Returns(_blockTrackerRepoMock.Object);

        _battleTxTracker = new BattleTxTracker(_loggerMock.Object, _serviceProviderMock.Object);
    }

    [Fact]
    public async Task ProcessTransactionsAsync_WithDifferentBlockDiffs_SetsCorrectLimit()
    {
        // Arrange
        const long currentBlockIndex = 1000;
        const long lastProcessedBlock = 990;

        _seasonCacheRepoMock.Setup(x => x.GetBlockIndexAsync()).ReturnsAsync(currentBlockIndex);
        _blockTrackerRepoMock.Setup(x => x.GetBattleTxTrackerBlockIndexAsync()).ReturnsAsync(lastProcessedBlock);

        var txsResponse = CreateSuccessGetTxsResponse();
        _headlessClientMock.Setup(x => x.GetTxs.ExecuteAsync(
                It.IsAny<long>(), 
                It.IsAny<int>(), 
                It.IsAny<string>(), 
                It.IsAny<List<TxStatus>>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(txsResponse);

        // Mock JWT validation
        var payload = new Dictionary<string, object> { ["bid"] = 1 };
        _battleTokenValidatorMock.Setup(x => x.TryValidateBattleToken(It.IsAny<string>(), out payload))
            .Returns(true);

        // Mock battle repository
        _battleRepoMock.Setup(x => x.UpdateBattle(It.IsAny<int>(), It.IsAny<Action<ArenaService.Shared.Models.Battle>>()))
            .ReturnsAsync(new ArenaService.Shared.Models.Battle());

        // Act
        await InvokeProcessTransactionsAsync(10);

        // Assert
        _blockTrackerRepoMock.Verify(x => x.SetBattleTxTrackerBlockIndexAsync(lastProcessedBlock + 1), Times.Once);
    }

    [Fact]
    public async Task ProcessTransactionsAsync_BlockDiffGreaterThan10_SetsLimitTo5()
    {
        // Arrange
        const long currentBlockIndex = 1020;
        const long lastProcessedBlock = 1000;

        _seasonCacheRepoMock.Setup(x => x.GetBlockIndexAsync()).ReturnsAsync(currentBlockIndex);
        _blockTrackerRepoMock.Setup(x => x.GetBattleTxTrackerBlockIndexAsync()).ReturnsAsync(lastProcessedBlock);

        var txsResponse = CreateSuccessGetTxsResponse();
        _headlessClientMock.Setup(x => x.GetTxs.ExecuteAsync(
                It.IsAny<long>(), 
                It.IsAny<int>(), 
                It.IsAny<string>(), 
                It.IsAny<List<TxStatus>>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(txsResponse);

        var payload = new Dictionary<string, object> { ["bid"] = 1 };
        _battleTokenValidatorMock.Setup(x => x.TryValidateBattleToken(It.IsAny<string>(), out payload))
            .Returns(true);

        _battleRepoMock.Setup(x => x.UpdateBattle(It.IsAny<int>(), It.IsAny<Action<ArenaService.Shared.Models.Battle>>()))
            .ReturnsAsync(new ArenaService.Shared.Models.Battle());

        // Act
        await InvokeProcessTransactionsAsync(20);

        // Assert
        _headlessClientMock.Verify(x => x.GetTxs.ExecuteAsync(
            It.IsAny<long>(),
            5,  // The limit should be 5
            It.IsAny<string>(),
            It.IsAny<List<TxStatus>>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);
        
        _blockTrackerRepoMock.Verify(x => x.SetBattleTxTrackerBlockIndexAsync(lastProcessedBlock + 5), Times.Once);
    }

    [Fact]
    public async Task ProcessTransactionsAsync_BlockDiffGreaterThan30_SetsLimitTo10()
    {
        // Arrange
        const long currentBlockIndex = 1040;
        const long lastProcessedBlock = 1000;

        _seasonCacheRepoMock.Setup(x => x.GetBlockIndexAsync()).ReturnsAsync(currentBlockIndex);
        _blockTrackerRepoMock.Setup(x => x.GetBattleTxTrackerBlockIndexAsync()).ReturnsAsync(lastProcessedBlock);

        var txsResponse = CreateSuccessGetTxsResponse();
        _headlessClientMock.Setup(x => x.GetTxs.ExecuteAsync(
                It.IsAny<long>(), 
                It.IsAny<int>(), 
                It.IsAny<string>(), 
                It.IsAny<List<TxStatus>>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(txsResponse);

        var payload = new Dictionary<string, object> { ["bid"] = 1 };
        _battleTokenValidatorMock.Setup(x => x.TryValidateBattleToken(It.IsAny<string>(), out payload))
            .Returns(true);

        _battleRepoMock.Setup(x => x.UpdateBattle(It.IsAny<int>(), It.IsAny<Action<ArenaService.Shared.Models.Battle>>()))
            .ReturnsAsync(new ArenaService.Shared.Models.Battle());

        // Act
        await InvokeProcessTransactionsAsync(40);

        // Assert
        _headlessClientMock.Verify(x => x.GetTxs.ExecuteAsync(
            It.IsAny<long>(),
            10,  // The limit should be 10
            It.IsAny<string>(),
            It.IsAny<List<TxStatus>>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);
        
        _blockTrackerRepoMock.Verify(x => x.SetBattleTxTrackerBlockIndexAsync(lastProcessedBlock + 10), Times.Once);
    }

    [Fact]
    public async Task ProcessTransactionsAsync_BlockDiffGreaterThan50_SetsLimitTo30()
    {
        // Arrange
        const long currentBlockIndex = 1060;
        const long lastProcessedBlock = 1000;

        _seasonCacheRepoMock.Setup(x => x.GetBlockIndexAsync()).ReturnsAsync(currentBlockIndex);
        _blockTrackerRepoMock.Setup(x => x.GetBattleTxTrackerBlockIndexAsync()).ReturnsAsync(lastProcessedBlock);

        var txsResponse = CreateSuccessGetTxsResponse();
        _headlessClientMock.Setup(x => x.GetTxs.ExecuteAsync(
                It.IsAny<long>(), 
                It.IsAny<int>(), 
                It.IsAny<string>(), 
                It.IsAny<List<TxStatus>>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(txsResponse);

        var payload = new Dictionary<string, object> { ["bid"] = 1 };
        _battleTokenValidatorMock.Setup(x => x.TryValidateBattleToken(It.IsAny<string>(), out payload))
            .Returns(true);

        _battleRepoMock.Setup(x => x.UpdateBattle(It.IsAny<int>(), It.IsAny<Action<ArenaService.Shared.Models.Battle>>()))
            .ReturnsAsync(new ArenaService.Shared.Models.Battle());

        // Act
        await InvokeProcessTransactionsAsync(60);

        // Assert
        _headlessClientMock.Verify(x => x.GetTxs.ExecuteAsync(
            It.IsAny<long>(),
            30,  // The limit should be 30
            It.IsAny<string>(),
            It.IsAny<List<TxStatus>>(),
            It.IsAny<CancellationToken>()
        ), Times.Once);
        
        _blockTrackerRepoMock.Verify(x => x.SetBattleTxTrackerBlockIndexAsync(lastProcessedBlock + 30), Times.Once);
    }

    private async Task InvokeProcessTransactionsAsync(long blockDiff)
    {
        // 리플렉션을 사용하여 private 메소드 호출
        var methodInfo = typeof(BattleTxTracker).GetMethod("ProcessTransactionsAsync", 
            BindingFlags.NonPublic | BindingFlags.Instance);
        
        await (Task)methodInfo.Invoke(_battleTxTracker, new object[] {
            _headlessClientMock.Object,
            _databaseMock.Object,
            _seasonCacheRepoMock.Object,
            _battleRepoMock.Object,
            _jobClientMock.Object,
            _battleTokenValidatorMock.Object,
            _cancellationTokenSource.Token
        });
    }

    private object CreateSuccessGetTxsResponse()
    {
        // 실제 응답 구조에 맞게 동적 객체 생성
        var actionRaw = CreateBattleActionRaw();
        
        // 간단한 익명 타입으로 응답 구조 모방
        return new
        {
            Data = new
            {
                Transaction = new
                {
                    NcTransactions = new[]
                    {
                        new
                        {
                            Id = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef",
                            Actions = new[]
                            {
                                new
                                {
                                    Raw = actionRaw
                                }
                            }
                        }
                    }
                }
            }
        };
    }

    private string CreateBattleActionRaw()
    {
        var codec = new Codec();
        var dictionary = new Dictionary
        {
            ["type_id"] = "battle".Serialize(),
            ["values"] = new Dictionary
            {
                ["memo"] = "battle_token".Serialize(),
                ["arena_provider"] = "test_provider".Serialize(),
                ["sender"] = new Address().Serialize(),
                ["costumes"] = Bencodex.Types.List.Empty.Add("costume1".Serialize()),
                ["equipments"] = Bencodex.Types.List.Empty.Add("equipment1".Serialize())
            }.Serialize()
        };

        var bytes = codec.Encode(dictionary);
        return Convert.ToHexString(bytes).ToLower();
    }
} 