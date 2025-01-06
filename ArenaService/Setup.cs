namespace ArenaService;

using ArenaService.Auth;
using ArenaService.Data;
using ArenaService.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
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

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapSwagger();
            endpoints.MapHealthChecks("/ping");
        });
    }
}
