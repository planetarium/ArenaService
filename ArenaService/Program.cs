using ArenaService.Data;
using ArenaService.Repositories;
using ArenaService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<ArenaDbContext>(options =>
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
        .UseSnakeCaseNamingConvention()
);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<ISeasonRepository, SeasonRepository>();
builder.Services.AddScoped<SeasonService>();
builder.Services.AddScoped<IParticipantRepository, ParticipantRepository>();
builder.Services.AddScoped<ParticipantService>();
builder.Services.AddScoped<IAvailableOpponentRepository, AvailableOpponentRepository>();
builder.Services.AddScoped<AvailableOpponentService>();
builder.Services.AddScoped<IBattleLogRepository, BattleLogRepository>();
builder.Services.AddScoped<ILeaderboardRepository, LeaderboardRepository>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowAllOrigins",
        builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
    );
});

var app = builder.Build();

app.UseDeveloperExceptionPage();
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseCors("AllowAllOrigins");

app.MapControllers();

app.Run();
