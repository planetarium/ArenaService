using ArenaService;
using SentryOptions = ArenaService.Options.SentryOptions;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseSentry(o =>
{
    var sentrySection = builder.Configuration.GetSection(SentryOptions.SectionName);
    var sentryOptions = sentrySection.Get<SentryOptions>();

    if (sentryOptions != null && sentryOptions.Enabled && !string.IsNullOrEmpty(sentryOptions.Dsn))
    {
        o.Dsn = sentryOptions.Dsn;
        o.Debug = sentryOptions.Debug;

        if (!string.IsNullOrEmpty(sentryOptions.Environment))
        {
            o.Environment = sentryOptions.Environment;
        }
    }
});

var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

var app = builder.Build();

var env = app.Services.GetRequiredService<IWebHostEnvironment>();
startup.Configure(app, env, app.Services);

app.Run();
