using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Imato.Dapper.DbContext.Test
{
    public static class AppBulder
    {
        private static IServiceProvider _provider = null!;

        public static void SetupApp()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            var builder = Host.CreateDefaultBuilder();
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IConfiguration>(config);
                services.AddSingleton<DbContext>();
            });
            builder.ConfigureLogging((_, logging) =>
            {
                logging.AddConsole();
            });
            var app = builder.Build();
            _provider = app.Services.CreateScope().ServiceProvider;
        }

        public static T GetRequiredService<T>() where T : class
        {
            return _provider.GetRequiredService<T>();
        }

        public static MethodInfo GetMethod<T>(string name,
                object[]? parameters = null)
        {
            if (parameters?.Length > 0)
            {
                return typeof(T)
                    .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(x => x.Name == name && x.GetParameters().Length == parameters.Length)
                    .FirstOrDefault() ?? throw new NotExistsMethodException<T>(name);
            }

            return typeof(T).GetMethod(name,
                BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new NotExistsMethodException<T>(name);
        }
    }
}