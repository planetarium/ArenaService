namespace ArenaService;

using System.Text.Json.Serialization;
using ArenaService.Auth;
using ArenaService.Data;
using ArenaService.Filter;
using ArenaService.JsonConverters;
using ArenaService.Options;
using ArenaService.Repositories;
using ArenaService.Services;
using ArenaService.Worker;
using Hangfire;
using Hangfire.Redis.StackExchange;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Converters;
using StackExchange.Redis;

public class Startup
{
    public IConfiguration Configuration { get; }

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

        services
            .AddHeadlessClient()
            .ConfigureHttpClient(
                (provider, client) =>
                {
                    var headlessOptions = provider.GetRequiredService<IOptions<HeadlessOptions>>();
                    client.BaseAddress = headlessOptions.Value.HeadlessEndpoint;

                    // if (
                    //     headlessStateServiceOption.Value.JwtSecretKey is not null
                    //     && headlessStateServiceOption.Value.JwtIssuer is not null
                    // )
                    // {
                    //     var key = new SymmetricSecurityKey(
                    //         Encoding.UTF8.GetBytes(headlessStateServiceOption.Value.JwtSecretKey)
                    //     );
                    //     var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                    //     var token = new JwtSecurityToken(
                    //         issuer: headlessStateServiceOption.Value.JwtIssuer,
                    //         expires: DateTime.UtcNow.AddMinutes(5),
                    //         signingCredentials: creds
                    //     );

                    //     client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    //         "Bearer",
                    //         new JwtSecurityTokenHandler().WriteToken(token)
                    //     );
                    // }
                }
            );

        services
            .AddControllers(options =>
            {
                options.Filters.Add<CacheExceptionFilter>();
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

        services.AddDbContext<ArenaDbContext>(options =>
            options
                .UseNpgsql(Configuration.GetConnectionString("DefaultConnection"))
                .UseSnakeCaseNamingConvention()
        );

        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var redisOptions = provider.GetRequiredService<IOptions<RedisOptions>>().Value;
            return ConnectionMultiplexer.Connect(
                $"{redisOptions.Host}:{redisOptions.Port},defaultDatabase={redisOptions.RankingDbNumber}"
            );
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
        services.AddScoped<IClanRankingRepository, ClanRankingRepository>();
        services.AddScoped<IClanRepository, ClanRepository>();

        services.AddScoped<IRankingRepository, RankingRepository>();
        services.AddScoped<ISeasonCacheRepository, SeasonCacheRepository>();

        services.AddScoped<IRankingService, RankingService>();
        services.AddScoped<ISeasonService, SeasonService>();
        services.AddScoped<ITxTrackingService, TxTrackingService>();
        services.AddScoped<IParticipateService, ParticipateService>();
        services.AddScoped<ISpecifyOpponentsService, SpecifyOpponentsService>();

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
                config.UseRedisStorage(
                    $"{redisOptions.Host}:{redisOptions.Port},defaultDatabase={redisOptions.HangfireDbNumber}",
                    new RedisStorageOptions { Prefix = redisOptions.HangfirePrefix }
                );
            }
        );

        services.AddSingleton<IBackgroundJobClient, BackgroundJobClient>();
        services
            .AddSingleton<RoundSyncWorker>()
            .AddHostedService(provider => provider.GetRequiredService<RoundSyncWorker>());

        services.AddHangfireServer();
        services.AddHealthChecks();
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

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseCors("AllowAllOrigins");
        app.UseRouting();
        app.UseAuthorization();

        app.UseHangfireDashboard(
            "/hangfire",
            new DashboardOptions { Authorization = [new AllowAllDashboardAuthorizationFilter()] }
        );

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapSwagger();
            endpoints.MapHealthChecks("/ping");
        });
    }
}

public class AllowAllDashboardAuthorizationFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(Hangfire.Dashboard.DashboardContext context)
    {
        return true;
    }
}
