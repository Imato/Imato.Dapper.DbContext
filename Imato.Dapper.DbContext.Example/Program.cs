using Imato.Dapper.DbContext;
using Imato.Dapper.DbContext.Example;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public static class Program
{
    public static ContextFactory Factory;

    public static void Main(string[] args)
    {
        var appBuilder = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddTransient<ContextFactory>();
                services.AddTransient<AppService>();
                services.AddLogging();
            })
            .ConfigureLogging(logging =>
            {
                logging.AddConsole();
            });

        var app = appBuilder.Build();
        Factory = app.Services.GetRequiredService<ContextFactory>();
        app.Services.GetRequiredService<AppService>()?.RunAsync().Wait();
    }
}