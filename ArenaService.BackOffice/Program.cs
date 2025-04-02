using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text;
using ArenaService.BackOffice.Components;
using ArenaService.BackOffice.Options;
using ArenaService.Options;
using ArenaService.Shared.Data;
using ArenaService.Shared.Jwt;
using ArenaService.Shared.Repositories;
using ArenaService.Shared.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
builder.Services.Configure<GoogleAuthOptions>(
    configuration.GetSection(GoogleAuthOptions.SectionName)
);
builder.Services.Configure<HeadlessOptions>(configuration.GetSection(HeadlessOptions.SectionName));

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

builder.Services
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

builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
{
    var redisOptions = provider.GetRequiredService<IOptions<RedisOptions>>().Value;
    var config = new ConfigurationOptions
    {
        DefaultDatabase = redisOptions.RankingDbNumber
    };

    config.EndPoints.Add(redisOptions.Host, int.Parse(redisOptions.Port));

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

builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedProto
        | ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseForwardedHeaders();

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

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
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
