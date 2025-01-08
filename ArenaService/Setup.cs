namespace ArenaService;

using ArenaService.Auth;
using ArenaService.Data;
using ArenaService.Options;
using ArenaService.Repositories;
using Hangfire;
using Hangfire.Redis.StackExchange;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

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

        services.AddControllers();

        services
            .AddAuthentication("ES256K")
            .AddScheme<AuthenticationSchemeOptions, ES256KAuthenticationHandler>("ES256K", null);

        services.AddDbContext<ArenaDbContext>(options =>
            options
                .UseNpgsql(Configuration.GetConnectionString("DefaultConnection"))
                .UseSnakeCaseNamingConvention()
        );
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
        });

        services.AddScoped<ISeasonRepository, SeasonRepository>();
        services.AddScoped<IParticipantRepository, ParticipantRepository>();
        services.AddScoped<IAvailableOpponentRepository, AvailableOpponentRepository>();
        services.AddScoped<IBattleLogRepository, BattleLogRepository>();
        services.AddScoped<ILeaderboardRepository, LeaderboardRepository>();

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
                    $"{redisOptions.Host}:{redisOptions.Port}",
                    new RedisStorageOptions { Prefix = redisOptions.Prefix }
                );
            }
        );

        services.AddSingleton<IBackgroundJobClient, BackgroundJobClient>();

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

        app.UseHangfireDashboard("/hangfire");

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapSwagger();
            // endpoints.MapHealthChecks("/ping");
        });
    }
}
