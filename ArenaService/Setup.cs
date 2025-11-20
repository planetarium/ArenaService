namespace ArenaService;

using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;
using ArenaService.Auth;
using ArenaService.Filter;
using ArenaService.JsonConverters;
using ArenaService.Options;
using ArenaService.Services;
using ArenaService.Shared.Data;
using ArenaService.Shared.Exceptions;
using ArenaService.Shared.Jwt;
using ArenaService.Shared.Repositories;
using ArenaService.Shared.Services;
using ArenaService.Utils;
using ArenaService.Worker;
using Hangfire;
using Hangfire.Redis.StackExchange;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Converters;
using Npgsql;
using StackExchange.Redis;

public class Startup
{
    public IConfiguration Configuration { get; }
    private SshTunnel? _sshTunnel;

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.Configure<RedisOptions>(Configuration.GetSection(RedisOptions.SectionName));
        services.Configure<HeadlessOptions>(Configuration.GetSection(HeadlessOptions.SectionName));
        services.Configure<OpsConfigOptions>(
            Configuration.GetSection(OpsConfigOptions.SectionName)
        );
        services.Configure<SshOptions>(Configuration.GetSection(SshOptions.SectionName));
        services.Configure<SentryOptions>(Configuration.GetSection(SentryOptions.SectionName));

        services
            .AddHeadlessClient()
            .ConfigureHttpClient(
                (provider, client) =>
                {
                    var headlessOptions = provider.GetRequiredService<IOptions<HeadlessOptions>>();
                    client.BaseAddress = headlessOptions.Value.HeadlessEndpoint;

                    if (
                        headlessOptions.Value.JwtSecretKey is not null
                        && headlessOptions.Value.JwtIssuer is not null
                    )
                    {
                        var key = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(headlessOptions.Value.JwtSecretKey)
                        );
                        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                        var token = new JwtSecurityToken(
                            issuer: headlessOptions.Value.JwtIssuer,
                            expires: DateTime.UtcNow.AddMinutes(5),
                            signingCredentials: creds
                        );
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                            "Bearer",
                            new JwtSecurityTokenHandler().WriteToken(token)
                        );
                    }
                }
            );

        services
            .AddControllers(options =>
            {
                options.Filters.Add<CacheExceptionFilter>();
                options.Filters.Add<NotRegisteredUserExceptionFilter>();
                options.Filters.Add<NotEnoughMedalExceptionFilter>();
                options.Filters.Add<CalcAOFailedExceptionFilter>();
            })
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.Converters.Add(new StringEnumConverter());
                options.SerializerSettings.Converters.Add(new TxIdJsonConverter());
                options.SerializerSettings.Converters.Add(new AddressJsonConverter());
            });

        services
            .AddAuthentication("ES256K")
            .AddScheme<AuthenticationSchemeOptions, ES256KAuthenticationHandler>("ES256K", null);

        SetupSshTunneling(services);

        services.AddDbContext<ArenaDbContext>(options =>
        {
            var connectionString = Configuration.GetConnectionString("DefaultConnection");

            if (_sshTunnel != null)
            {
                var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
                builder.Host = "127.0.0.1";
                connectionString = builder.ConnectionString;
            }

            options.UseNpgsql(connectionString, 
                    npgsqlOptions => npgsqlOptions.MigrationsAssembly("ArenaService.Shared"))
                .UseSnakeCaseNamingConvention();
        });

        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var redisOptions = provider.GetRequiredService<IOptions<RedisOptions>>().Value;

            var config = new ConfigurationOptions
            {
                DefaultDatabase = redisOptions.RankingDbNumber
            };

            if (_sshTunnel != null)
            {
                config.EndPoints.Add("127.0.0.1", int.Parse(redisOptions.Port));
            }
            else
            {
                config.EndPoints.Add(redisOptions.Host, int.Parse(redisOptions.Port));
            }

            if (!string.IsNullOrEmpty(redisOptions.Username))
            {
                config.User = redisOptions.Username;
            }

            if (!string.IsNullOrEmpty(redisOptions.Password))
            {
                config.Password = redisOptions.Password;
            }

            return ConnectionMultiplexer.Connect(config);
        });

        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(
                "v1",
                new OpenApiInfo { Title = "ArenaService API", Version = "v1" }
            );

            options.AddSecurityDefinition(
                "BearerAuth",
                new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description =
                        "Enter 'Bearer' followed by a space and the JWT. Example: 'Bearer your-token'"
                }
            );

            options.OperationFilter<AuthorizeCheckOperationFilter>();
            options.SchemaFilter<TxIdSchemaFilter>();

            options.EnableAnnotations();
        });
        services.AddSwaggerGenNewtonsoftSupport();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISeasonRepository, SeasonRepository>();
        services.AddScoped<IParticipantRepository, ParticipantRepository>();
        services.AddScoped<IBattleRepository, BattleRepository>();
        services.AddScoped<IAvailableOpponentRepository, AvailableOpponentRepository>();
        services.AddScoped<IRoundRepository, RoundRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IMedalRepository, MedalRepository>();
        services.AddScoped<ILeaderboardRepository, LeaderboardRepository>();
        services.AddScoped<IClanRepository, ClanRepository>();
        services.AddScoped<IRankingSnapshotRepository, RankingSnapshotRepository>();

        services.AddScoped<IClanRankingRepository, ClanRankingRepository>();
        services.AddScoped<IAllClanRankingRepository, AllClanRankingRepository>();
        services.AddScoped<IRankingRepository, RankingRepository>();
        services.AddScoped<ISeasonCacheRepository, SeasonCacheRepository>();
        services.AddScoped<IBlockTrackerRepository, BlockTrackerRepository>();

        services.AddScoped<ISeasonPreparationService, SeasonPreparationService>();
        services.AddScoped<IRoundPreparationService, RoundPreparationService>();
        services.AddScoped<IRankingService, RankingService>();
        services.AddScoped<ISeasonService, SeasonService>();
        services.AddScoped<ITxTrackingService, TxTrackingService>();
        services.AddScoped<IParticipateService, ParticipateService>();

        services.AddCors(options =>
        {
            options.AddPolicy(
                "AllowAllOrigins",
                builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
            );
        });

        services.AddHangfire(
            (provider, config) =>
            {
                var redisOptions = provider.GetRequiredService<IOptions<RedisOptions>>().Value;

                var redisConfig = new ConfigurationOptions
                {
                    DefaultDatabase = redisOptions.HangfireDbNumber
                };

                if (_sshTunnel != null)
                {
                    redisConfig.EndPoints.Add("127.0.0.1", int.Parse(redisOptions.Port));
                }
                else
                {
                    redisConfig.EndPoints.Add(redisOptions.Host, int.Parse(redisOptions.Port));
                }

                if (!string.IsNullOrEmpty(redisOptions.Username))
                {
                    redisConfig.User = redisOptions.Username;
                }

                if (!string.IsNullOrEmpty(redisOptions.Password))
                {
                    redisConfig.Password = redisOptions.Password;
                }

                config.UseRedisStorage(
                    ConnectionMultiplexer.Connect(redisConfig),
                    new RedisStorageOptions
                    {
                        Prefix = redisOptions.HangfirePrefix,
                        Db = redisOptions.HangfireDbNumber
                    }
                );
            }
        );

        services.AddSingleton<IBackgroundJobClient, BackgroundJobClient>();
        var opsConfig = Configuration
            .GetSection(OpsConfigOptions.SectionName)
            .Get<OpsConfigOptions>();

        services.AddSingleton(new BattleTokenGenerator(opsConfig!.JwtSecretKey));
        services.AddSingleton(new BattleTokenValidator(opsConfig!.JwtPublicKey));
        services
            .AddSingleton<CacheBlockTipWorker>()
            .AddHostedService(provider => provider.GetRequiredService<CacheBlockTipWorker>());
        services
            .AddSingleton<PrepareRankingWorker>()
            .AddHostedService(provider => provider.GetRequiredService<PrepareRankingWorker>());
        services
            .AddSingleton<RankingCopyWorker>()
            .AddHostedService(provider => provider.GetRequiredService<RankingCopyWorker>());
        // services
        //     .AddSingleton<AllClanRankingWorker>()
        //     .AddHostedService(provider => provider.GetRequiredService<AllClanRankingWorker>());
        services
            .AddSingleton<BattleTxTracker>()
            .AddHostedService(provider => provider.GetRequiredService<BattleTxTracker>());

        services.AddHangfireServer();
        services.AddHealthChecks();
       
    }

    private void SetupSshTunneling(IServiceCollection services)
    {
        var sshOptions = Configuration.GetSection(SshOptions.SectionName).Get<SshOptions>();

        if (sshOptions?.Enabled == true)
        {
            var tempServiceProvider = services.BuildServiceProvider();
            var loggerFactory = tempServiceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<SshTunnel>();

            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
            string dbHost = builder.Host;
            int dbPort = builder.Port;

            var redisOptions = Configuration
                .GetSection(RedisOptions.SectionName)
                .Get<RedisOptions>();
            string redisHost = redisOptions.Host;
            int redisPort = int.Parse(redisOptions.Port);

            _sshTunnel = new SshTunnel(
                sshOptions.Host,
                sshOptions.Port,
                sshOptions.Username,
                sshOptions.Password,
                dbHost,
                dbPort,
                redisHost,
                redisPort,
                logger
            );

            _sshTunnel.Start();

            logger.LogInformation("SSH tunneling enabled successfully");
        }
    }

    public void Configure(
        IApplicationBuilder app,
        IWebHostEnvironment env,
        IServiceProvider serviceProvider
    )
    {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseHttpsRedirection();
        app.UseCors("AllowAllOrigins");

        app.UseHangfireDashboard(
            "/hangfire",
            new DashboardOptions
            {
                Authorization = new[]
                {
                    new BasicAuthDashboardAuthorizationFilter(
                        serviceProvider.GetRequiredService<IOptions<OpsConfigOptions>>()
                    )
                }
            }
        );

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapSwagger();
            endpoints.MapHealthChecks("/ping");
        });

        var lifetime = serviceProvider.GetRequiredService<IHostApplicationLifetime>();
        lifetime.ApplicationStopping.Register(() =>
        {
            if (_sshTunnel != null)
            {
                _sshTunnel.Dispose();
                serviceProvider
                    .GetRequiredService<ILogger<Startup>>()
                    .LogInformation("SSH tunnel disposed successfully");
            }
        });
    }
}
