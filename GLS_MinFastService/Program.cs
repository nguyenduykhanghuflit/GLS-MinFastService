using GLS_MinFastService.Helpers;
using GLS_MinFastService.Workers;
using Serilog;
IHost host = Host.CreateDefaultBuilder(args)
    .UseSerilog()
    .UseWindowsService(o =>
    {
        o.ServiceName = "GLS Min-Fast service";
    })
    .ConfigureServices((hostContext, services) =>
    {
        //config for Serilog
        Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(hostContext.Configuration)
                      .CreateLogger();

        services.AddHostedService<Worker>();

        services.AddSingleton(hostContext.Configuration);
        //DI
        services.AddTransient<SqlHelper>();
        services.AddTransient<Config>();
    })
    .Build();

await host.RunAsync();
