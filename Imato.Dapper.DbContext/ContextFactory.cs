using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Imato.Dapper.DbContext
{
    public class ContextFactory
    {
        private readonly List<IAppDbContext> _contexts = new List<IAppDbContext>();
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;

        public ContextFactory(
            IConfiguration configuration,
            ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger(nameof(ContextFactory));
            LoadContexts(configuration);
        }

        private void LoadContexts(IConfiguration configuration)
        {
            foreach (var config in configuration
                .GetSection("ConnectionStrings")
                .GetChildren())
            {
                var context = SelectContext(config.Key, config.Value ?? "null");
                if (context != null)
                {
                    _logger?.LogDebug($"Add context {context.Provider} {context.Name}");
                    _contexts.Add(context);
                }
            }
        }

        private IEnumerable<IAppDbContext> CreateContexts(
            string name,
            string connectionString)
        {
            return new List<IAppDbContext>
            {
                new MsSqlContext(
                   _loggerFactory.CreateLogger(nameof(MsSqlContext)),
                    connectionString,
                    name),
                new PostgresContext(
                    _loggerFactory.CreateLogger(nameof(PostgresContext)),
                    connectionString,
                    name),
                new MySqlContext(
                    _loggerFactory.CreateLogger(nameof(MySqlContext)),
                    connectionString,
                    name)
            };
        }

        private IAppDbContext SelectContext(string name, string connectionString)
        {
            _logger?.LogDebug($"Select context for string {name}");

            foreach (var context in CreateContexts(name, connectionString))
            {
                if (context.Name.ToLower() == context.Provider.ToString()
                    || context.IsMyConnectionString(connectionString))
                {
                    return context;
                }
            }

            throw new ArgumentException($"Unknown connection string {name} provider. {connectionString}");
        }

        public IAppDbContext GetDbContext()
        {
            _logger?.LogDebug("Get DbContext");
            return _contexts
                .Where(x => x.IsActive)
                .First();
        }

        public IAppDbContext GetDbContext(ContextProviders provider)
        {
            _logger?.LogDebug($"Get DbContext {provider}");

            return _contexts
                .Where(x => x.IsActive && x.Provider == provider)
                .First();
        }

        public IAppDbContext GetDbContext(string name)
        {
            _logger?.LogDebug($"Get DbContext {name}");

            return _contexts
                .Where(x => x.IsActive && x.Name == name)
                .First();
        }
    }
}