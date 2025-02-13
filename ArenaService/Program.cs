using ArenaService;

var builder = WebApplication.CreateBuilder(args);

var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

var app = builder.Build();

var env = app.Services.GetRequiredService<IWebHostEnvironment>();
startup.Configure(app, env, app.Services);

app.Run();
