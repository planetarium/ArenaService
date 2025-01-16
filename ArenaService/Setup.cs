namespace ArenaService;

using System.Text.Json.Serialization;
using ArenaService.Auth;
using ArenaService.Data;
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
            .AddControllers()
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter())
            );

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

            options.EnableAnnotations();
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISeasonRepository, SeasonRepository>();
        services.AddScoped<IParticipantRepository, ParticipantRepository>();
        services.AddScoped<IBattleLogRepository, BattleLogRepository>();
        services.AddScoped<IAvailableOpponentRepository, AvailableOpponentRepository>();
        services.AddScoped<IRoundRepository, RoundRepository>();

        services.AddScoped<IRankingRepository, RankingRepository>();
        services.AddScoped<ISeasonCacheRepository, SeasonCacheRepository>();

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
                config.UseRedisStorage(
                    $"{redisOptions.Host}:{redisOptions.Port},defaultDatabase={redisOptions.HangfireDbNumber}",
                    new RedisStorageOptions { Prefix = redisOptions.HangfirePrefix }
                );
            }
        );

        services.AddSingleton<IBackgroundJobClient, BackgroundJobClient>();
        // services
        //     .AddSingleton<SeasonCachingWorker>()
        //     .AddHostedService(provider => provider.GetRequiredService<SeasonCachingWorker>());

        services.AddHangfireServer();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
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
            // endpoints.MapHealthChecks("/ping");
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
