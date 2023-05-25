using Npgsql;
using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Imato.Dapper.DbContext
{
    public class PostgresContext : BaseAppDbContext
    {
        public override ContextProviders Provider => ContextProviders.postgres;

        public PostgresContext(ILogger logger, string connectionString, string? name = null)
            : base(logger, connectionString, name)
        {
        }

        protected override IDbConnection CreateConnection(string dbName = "")
        {
            if (string.IsNullOrEmpty(dbName))
            {
                return new NpgsqlConnection(connectionString);
            }
            var sb = new NpgsqlConnectionStringBuilder(connectionString);
            sb.Database = dbName;
            return new NpgsqlConnection(sb.ConnectionString);
        }

        public override string GetDbName()
        {
            if (dbName == null)
            {
                var sb = new NpgsqlConnectionStringBuilder(connectionString);
                dbName = sb.Database ?? "";
            }
            return dbName;
        }

        public override bool IsMasterServer(string serverName = "")
        {
            const string sql = @"
create temp table tt_cmd (hostname text);
copy tt_cmd from program 'hostname';
select * from tt_cmd;
drop table tt_cmd;";

            serverName = string.IsNullOrEmpty(serverName) ? Environment.MachineName : serverName;
            using (var connection = GetConnection("postgres"))
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
(select true as result from pg_database d where d.datname = @name
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
            return connectionString.Contains("Host")
                && connectionString.Contains("Database");
        }
    }
}