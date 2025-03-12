using Renci.SshNet;

namespace ArenaService.Utils
{
    public class SshTunnel : IDisposable
    {
        private readonly SshClient _sshClient;
        private readonly ForwardedPortLocal _dbTunnel;
        private readonly ForwardedPortLocal _redisTunnel;
        private readonly ILogger<SshTunnel> _logger;
        private bool _isDisposed;
        private Timer _keepAliveTimer;
        private readonly object _reconnectLock = new object();
        private const int KeepAliveInterval = 60000;
        private const int ReconnectAttempts = 5;
        private const int ReconnectDelay = 5000;

        public int LocalDbPort { get; }
        public int LocalRedisPort { get; }

        public SshTunnel(
            string sshHostname,
            int sshPort,
            string sshUsername,
            string password,
            string dbHost,
            int dbPort,
            string redisHost,
            int redisPort,
            ILogger<SshTunnel> logger
        )
        {
            _logger = logger;

            try
            {
                LocalDbPort = dbPort;
                LocalRedisPort = redisPort;

                _sshClient = new SshClient(sshHostname, sshPort, sshUsername, password);

                _dbTunnel = new ForwardedPortLocal(
                    "127.0.0.1",
                    (uint)LocalDbPort,
                    dbHost,
                    (uint)dbPort
                );

                _redisTunnel = new ForwardedPortLocal(
                    "127.0.0.1",
                    (uint)LocalRedisPort,
                    redisHost,
                    (uint)redisPort
                );

                _logger.LogInformation("SSH tunnels created for DB and Redis");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating SSH tunnel");
                throw;
            }
        }

        public void Start()
        {
            if (!_sshClient.IsConnected)
            {
                ConnectAndStartTunnels();
            }

            _keepAliveTimer = new Timer(
                CheckConnectionStatus,
                null,
                KeepAliveInterval,
                KeepAliveInterval
            );
            _logger.LogInformation("SSH tunnel monitoring started");
        }

        private void ConnectAndStartTunnels()
        {
            try
            {
                if (!_sshClient.IsConnected)
                {
                    _sshClient.Connect();
                    _logger.LogInformation("SSH client connected");
                }

                if (!_dbTunnel.IsStarted)
                {
                    _sshClient.AddForwardedPort(_dbTunnel);
                    _dbTunnel.Start();
                    _logger.LogInformation($"DB tunnel started on local port {LocalDbPort}");
                }

                if (!_redisTunnel.IsStarted)
                {
                    _sshClient.AddForwardedPort(_redisTunnel);
                    _redisTunnel.Start();
                    _logger.LogInformation($"Redis tunnel started on local port {LocalRedisPort}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting and starting SSH tunnels");
                throw;
            }
        }

        private void CheckConnectionStatus(object state)
        {
            if (_isDisposed)
                return;

            try
            {
                // 연결 상태 확인
                bool needsReconnect =
                    !_sshClient.IsConnected || !_dbTunnel.IsStarted || !_redisTunnel.IsStarted;

                if (needsReconnect)
                {
                    _logger.LogWarning("SSH tunnel disconnected, attempting to reconnect...");
                    ReconnectWithRetry();
                }
                else
                {
                    _sshClient.SendKeepAlive();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during connection status check");
                ReconnectWithRetry();
            }
        }

        private void ReconnectWithRetry()
        {
            if (!Monitor.TryEnter(_reconnectLock, 0))
                return;

            try
            {
                int attempts = 0;
                bool reconnected = false;

                // 여러 번 재연결 시도
                while (!reconnected && attempts < ReconnectAttempts && !_isDisposed)
                {
                    attempts++;
                    _logger.LogInformation($"Reconnection attempt {attempts}/{ReconnectAttempts}");

                    try
                    {
                        CleanupConnections(false);

                        _sshClient.Connect();

                        if (!_dbTunnel.IsStarted)
                        {
                            _sshClient.AddForwardedPort(_dbTunnel);
                            _dbTunnel.Start();
                        }

                        if (!_redisTunnel.IsStarted)
                        {
                            _sshClient.AddForwardedPort(_redisTunnel);
                            _redisTunnel.Start();
                        }

                        reconnected = true;
                        _logger.LogInformation("SSH tunnel successfully reconnected");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Reconnection attempt {attempts} failed");

                        if (attempts < ReconnectAttempts)
                        {
                            _logger.LogInformation(
                                $"Waiting {ReconnectDelay / 1000} seconds before next attempt"
                            );
                            Thread.Sleep(ReconnectDelay);
                        }
                    }
                }

                if (!reconnected)
                {
                    _logger.LogError("All reconnection attempts failed");
                }
            }
            finally
            {
                Monitor.Exit(_reconnectLock);
            }
        }

        private void CleanupConnections(bool dispose)
        {
            try
            {
                if (_dbTunnel.IsStarted)
                {
                    _dbTunnel.Stop();
                }

                if (_redisTunnel.IsStarted)
                {
                    _redisTunnel.Stop();
                }

                if (_sshClient.IsConnected)
                {
                    _sshClient.Disconnect();
                }

                if (dispose)
                {
                    _sshClient.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during connection cleanup");
            }
        }

        public void Stop()
        {
            CleanupConnections(false);
            _logger.LogInformation("SSH tunnels stopped");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            if (disposing)
            {
                _keepAliveTimer?.Dispose();
                CleanupConnections(true);
                _logger.LogInformation("SSH tunnel disposed");
            }

            _isDisposed = true;
        }
    }
}
