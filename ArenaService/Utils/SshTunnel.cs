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
        private const int KeepAliveInterval = 30000;
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
                LocalDbPort = dbPort + 10000;
                LocalRedisPort = redisPort;

                _logger.LogInformation($"SSH tunnel will use local port {LocalDbPort} for DB and {LocalRedisPort} for Redis");

                _sshClient = new SshClient(sshHostname, sshPort, sshUsername, password);
                
                _dbTunnel = new ForwardedPortLocal(
                    "0.0.0.0",
                    (uint)LocalDbPort,
                    dbHost,
                    (uint)dbPort
                );

                _redisTunnel = new ForwardedPortLocal(
                    "0.0.0.0",
                    (uint)LocalRedisPort,
                    redisHost,
                    (uint)redisPort
                );

                _logger.LogInformation($"SSH tunnels created: DB {dbHost}:{dbPort} -> 0.0.0.0:{LocalDbPort}, Redis {redisHost}:{redisPort} -> 0.0.0.0:{LocalRedisPort}");
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
            _logger.LogInformation("SSH tunnel monitoring started with interval {interval}ms", KeepAliveInterval);
        }

        private void ConnectAndStartTunnels()
        {
            try
            {
                if (!_sshClient.IsConnected)
                {
                    _logger.LogInformation("Connecting to SSH server...");
                    _sshClient.Connect();
                    _logger.LogInformation("SSH client connected successfully");
                }

                if (!_dbTunnel.IsStarted)
                {
                    _logger.LogInformation("Starting DB tunnel...");
                    _sshClient.AddForwardedPort(_dbTunnel);
                    _dbTunnel.Start();
                    _logger.LogInformation($"DB tunnel started on local port {LocalDbPort}");
                }

                if (!_redisTunnel.IsStarted)
                {
                    _logger.LogInformation("Starting Redis tunnel...");
                    _sshClient.AddForwardedPort(_redisTunnel);
                    _redisTunnel.Start();
                    _logger.LogInformation($"Redis tunnel started on local port {LocalRedisPort}");
                }
                
                var testCmd = _sshClient.RunCommand("netstat -an | grep LISTEN");
                _logger.LogInformation("SSH server open ports: {output}", testCmd.Result);
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
                bool needsReconnect =
                    !_sshClient.IsConnected || !_dbTunnel.IsStarted || !_redisTunnel.IsStarted;
                
                _logger.LogDebug("SSH connection status: SSH Connected={sshConnected}, DB Tunnel={dbTunnel}, Redis Tunnel={redisTunnel}", 
                    _sshClient.IsConnected, _dbTunnel.IsStarted, _redisTunnel.IsStarted);

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
                    _logger.LogDebug("DB tunnel stopped");
                }

                if (_redisTunnel.IsStarted)
                {
                    _redisTunnel.Stop();
                    _logger.LogDebug("Redis tunnel stopped");
                }

                if (_sshClient.IsConnected)
                {
                    _sshClient.Disconnect();
                    _logger.LogDebug("SSH client disconnected");
                }

                if (dispose)
                {
                    _sshClient.Dispose();
                    _logger.LogDebug("SSH client disposed");
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
