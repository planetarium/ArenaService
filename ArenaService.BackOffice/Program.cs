using ArenaService.BackOffice.Options;
using ArenaService.Shared.Data;
using ArenaService.Shared.Repositories;
using ArenaService.Shared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

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
builder.Services.AddScoped<IClanRankingRepository, ClanRankingRepository>();
builder.Services.AddScoped<IMedalRepository, MedalRepository>();

builder.Services.AddScoped<ISeasonService, SeasonService>();
builder.Services.AddScoped<IRankingService, RankingService>();
builder.Services.AddScoped<ISeasonPreparationService, SeasonPreparationService>();
builder.Services.AddScoped<IRoundPreparationService, RoundPreparationService>();
builder.Services.AddScoped<ISeasonService, SeasonService>();

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

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
