using System.Data;

namespace Imato.Dapper.DbContext
{
    public interface IAppDbContext : IDisposable
    {
        string Name { get; }
        bool IsActive { get; }
        ContextProviders Provider { get; }

        IDbConnection GetConnection(string dbName = "", string connectionName = "");

        string GetDbName();

        bool IsMasterServer(string serverName = "");

        bool IsDbActive();

        bool IsMyConnectionString(string connectionString);
    }
}