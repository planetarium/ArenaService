namespace ArenaService.Admin;

using ArenaService.Admin.Options;
using ArenaService.Shared.Data;
using ArenaService.Shared.Repositories;
using ArenaService.Shared.Services;
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

        services
            .AddControllers()
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.Converters.Add(new StringEnumConverter());
            });

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

            options.EnableAnnotations();
        });
        services.AddSwaggerGenNewtonsoftSupport();

        services.AddCors(options =>
        {
            options.AddPolicy(
                "AllowAllOrigins",
                builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
            );
        });

        services.AddScoped<ISeasonRepository, SeasonRepository>();
        services.AddScoped<IRoundRepository, RoundRepository>();
        services.AddScoped<IParticipantRepository, ParticipantRepository>();
        services.AddScoped<IRankingSnapshotRepository, RankingSnapshotRepository>();
        services.AddScoped<IAllClanRankingRepository, AllClanRankingRepository>();
        services.AddScoped<IRankingRepository, RankingRepository>();
        services.AddScoped<IClanRepository, ClanRepository>();
        services.AddScoped<IClanRankingRepository, ClanRankingRepository>();
        services.AddScoped<IMedalRepository, MedalRepository>();
        services.AddScoped<ISeasonService, SeasonService>();
        services.AddScoped<IRankingService, RankingService>();

        services.AddScoped<ISeasonPreparationService, SeasonPreparationService>();
        services.AddScoped<IRoundPreparationService, RoundPreparationService>();

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

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseHttpsRedirection();
        app.UseCors("AllowAllOrigins");

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapSwagger();
            endpoints.MapHealthChecks("/ping");
        });
    }
}
