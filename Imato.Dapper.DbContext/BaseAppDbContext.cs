using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Data;

namespace Imato.Dapper.DbContext
{
    public abstract class BaseAppDbContext : IAppDbContext
    {
        protected readonly string? connectionString;
        protected string dbName = null!;
        protected readonly ConcurrentDictionary<string, IDbConnection> pool = new ConcurrentDictionary<string, IDbConnection>();
        protected readonly ILogger _logger;

        public virtual ContextProviders Provider => ContextProviders.unknown;
        public string Name { get; internal set; } = "Unknown";

        public bool IsActive => connectionString != null;

        public BaseAppDbContext(
            ILogger logger,
            string connectionString,
            string? name = null)
        {
            this.connectionString = connectionString;
            Name = name ?? Provider.ToString();
            _logger = logger;
        }

        protected abstract IDbConnection CreateConnection(string dbName = "");

        public abstract string GetDbName();

        public abstract bool IsMasterServer(string serverName = "");

        public abstract bool IsDbActive();

        public abstract bool IsMyConnectionString(string connectionString);

        protected bool IsReady(IDbConnection? connection)
        {
            return connection != null
                && connection.State != ConnectionState.Closed
                && connection.State != ConnectionState.Broken;
        }

        public IDbConnection GetConnection(string dbName = "", string connectionName = "")
        {
            var connection = CreateConnection(dbName);
            return pool.AddOrUpdate(
                connectionName,
                (_) => connection,
                (_, old) => IsReady(old) ? old : connection);
        }

        public void Dispose()
        {
            foreach (var p in pool)
            {
                if (p.Value.State != ConnectionState.Closed)
                {
                    _logger.LogDebug($"Close connection {p.Key}");
                    p.Value.Close();
                }
            }
        }
    }
}