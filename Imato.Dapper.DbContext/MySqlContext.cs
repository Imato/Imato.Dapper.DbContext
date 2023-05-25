using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace Imato.Dapper.DbContext
{
    public class MySqlContext : BaseAppDbContext
    {
        public override ContextProviders Provider => ContextProviders.mysql;

        public MySqlContext(ILogger logger, string connectionString, string? name = null)
            : base(logger, connectionString, name)
        {
        }

        protected override IDbConnection CreateConnection(string dbName = "")
        {
            if (string.IsNullOrEmpty(dbName))
            {
                return new MySqlConnection(connectionString);
            }
            var sb = new MySqlConnectionStringBuilder(connectionString);
            sb.Database = dbName;
            return new MySqlConnection(sb.ConnectionString);
        }

        public override string GetDbName()
        {
            if (dbName == null)
            {
                var sb = new MySqlConnectionStringBuilder(connectionString);
                dbName = sb.Database ?? "";
            }
            return dbName;
        }

        public override bool IsMasterServer(string serverName = "")
        {
            const string sql = "select @@hostname";

            serverName = string.IsNullOrEmpty(serverName) ? Environment.MachineName : serverName;
            using (var connection = GetConnection())
            {
                connection.Open();
                return connection.QueryFirst<string>(sql)
                    .StartsWith(
                        serverName,
                        StringComparison.OrdinalIgnoreCase);
            }
        }

        public override bool IsDbActive()
        {
            const string sqlStatus = @"
select result
from
(select true as result from information_schema.schemata where SCHEMA_NAME = @name
union all select false) t
order by 1 desc
limit 1";
            using (var connection = GetConnection())
            {
                connection.Open();
                return connection.QueryFirst<bool>(
                    sqlStatus,
                    new { name = GetDbName() });
            }
        }

        public override bool IsMyConnectionString(string connectionString)
        {
            return connectionString.Contains("Server")
                && connectionString.Contains("Database")
                && connectionString.Contains("Uid");
        }
    }
}