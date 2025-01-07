using ArenaService.Worker;
using ArenaService.Worker.Options;
using ArenaService.Worker.Rpc;
using Lib9c.Formatters;
using Lib9c.Renderers;
using MessagePack;
using MessagePack.Resolvers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<RpcConfigOptions>(
    builder.Configuration.GetSection(RpcConfigOptions.RpcConfig)
);

// builder.Services.AddHostedService<Worker>();
// builder.Services.AddHostedService<RpcNodeCheckService>().AddSingleton<RpcNodeHealthCheck>();
// builder.Services.AddSingleton<RpcClient>();
// builder.Services.AddSingleton<Receiver>();
// builder.Services.AddHostedService<RpcService>();
// builder.Services.AddSingleton<ActionRenderer>();

var resolver = CompositeResolver.Create(NineChroniclesResolver.Instance, StandardResolver.Instance);
var options = MessagePackSerializerOptions.Standard.WithResolver(resolver);
MessagePackSerializer.DefaultOptions = options;

var host = builder.Build();
host.Run();
