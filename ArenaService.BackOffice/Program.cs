using ArenaService.BackOffice.Components;
using ArenaService.BackOffice.Options;
using ArenaService.Shared.Data;
using ArenaService.Shared.Jwt;
using ArenaService.Shared.Repositories;
using ArenaService.Shared.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
builder.Services.Configure<GoogleAuthOptions>(
    configuration.GetSection(GoogleAuthOptions.SectionName)
);

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Add Google Authentication
builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddGoogle(googleOptions =>
    {
        var googleAuthOptions = builder
            .Services.BuildServiceProvider()
            .GetRequiredService<IOptions<GoogleAuthOptions>>()
            .Value;
        googleOptions.ClientId = googleAuthOptions.ClientId;
        googleOptions.ClientSecret = googleAuthOptions.ClientSecret;

        googleOptions.CallbackPath = "/signin-google";

        googleOptions.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
        googleOptions.CorrelationCookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddDbContext<ArenaDbContext>(options =>
    options
        .UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
        .UseSnakeCaseNamingConvention()
);

builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
{
    var redisOptions = provider.GetRequiredService<IOptions<RedisOptions>>().Value;
    return ConnectionMultiplexer.Connect(
        $"{redisOptions.Host}:{redisOptions.Port},defaultDatabase={redisOptions.RankingDbNumber}"
    );
});

builder.Services.AddScoped<ISeasonCacheRepository, SeasonCacheRepository>();
builder.Services.AddScoped<IBattleTicketPolicyRepository, BattleTicketPolicyRepository>();
builder.Services.AddScoped<IRefreshTicketPolicyRepository, RefreshTicketPolicyRepository>();
builder.Services.AddScoped<ISeasonRepository, SeasonRepository>();
builder.Services.AddScoped<IRoundRepository, RoundRepository>();
builder.Services.AddScoped<IParticipantRepository, ParticipantRepository>();
builder.Services.AddScoped<IRankingSnapshotRepository, RankingSnapshotRepository>();
builder.Services.AddScoped<IAllClanRankingRepository, AllClanRankingRepository>();
builder.Services.AddScoped<IRankingRepository, RankingRepository>();
builder.Services.AddScoped<IClanRepository, ClanRepository>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<IBattleRepository, BattleRepository>();
builder.Services.AddScoped<IClanRankingRepository, ClanRankingRepository>();
builder.Services.AddScoped<IMedalRepository, MedalRepository>();
builder.Services.AddScoped<ILeaderboardRepository, LeaderboardRepository>();

builder.Services.AddScoped<ISeasonService, SeasonService>();
builder.Services.AddScoped<IRankingService, RankingService>();
builder.Services.AddScoped<ISeasonPreparationService, SeasonPreparationService>();
builder.Services.AddScoped<IRoundPreparationService, RoundPreparationService>();

// Fake Key
builder.Services.AddSingleton(
    new BattleTokenGenerator(
        "LS0tLS1CRUdJTiBQVUJMSUMgS0VZLS0tLS0KTUlJQklqQU5CZ2txaGtpRzl3MEJBUUVGQUFPQ0FROEFNSUlCQ2dLQ0FRRUF1UkpPT0xhTGcrMHJyd20xNUdwMgpPWnRmMXdLeDB0dlZ1RSt0ZXFZUDZ3Zm1zTE5KZnpRcTVqYjZSVFhKU2FjRS9mN3JDQ013cnBqVUJtM2ZzTUxpCkZ1aE1ZY1IweTdYb1BHRCtHb1lXM0xYYVMwSC9RY1FMUmk5ejZKM1NyOC9UREZ6eVo0MGtlOHE2M0k1STNBWDYKSlBzUUZpNlZoNHl5MWtqZDJVTjdZazNQcjRWY3BCS1pxNnc4VFlnRWNhbWxCeXFxWWdkbjdtZjRySUtFREYvZQo3NHp1T0ZLcnE2Y0hJMGRuMGtpdTZBY3lkcWYxVUx3dDFRVHRNajdYN3h3WmRCZUNVV3AzOU16bTdDMzRjemF4CkMrZ05tQnVYZFQwT3NhMTlqZlJlRjB5cUpvNG1xMVpOMi9GY0xXZVZyYStVNEhZdXJqcTJqYjIyRWxiSDYxak8KOFFJREFRQUIKLS0tLS1FTkQgUFVCTElDIEtFWS0tLS0tCg=="
    )
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.Use(
    async (context, next) =>
    {
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
        await next();
    }
);

app.UseRouting();

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapGet(
    "/login",
    async (HttpContext context) =>
    {
        await context.ChallengeAsync(
            GoogleDefaults.AuthenticationScheme,
            new AuthenticationProperties { RedirectUri = "/" }
        );
    }
);

app.MapGet(
    "/logout",
    async (HttpContext context) =>
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return Results.Redirect("/");
    }
);

app.MapRazorComponents<App>().AddInteractiveServerRenderMode().RequireAuthorization();

app.Run();
