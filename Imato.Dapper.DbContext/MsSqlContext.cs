using System.Data.SqlClient;
using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Imato.Dapper.DbContext
{
    public class MsSqlContext : BaseAppDbContext
    {
        public override ContextProviders Provider => ContextProviders.mssql;

        public MsSqlContext(ILogger logger, string connectionString, string? name = null)
            : base(logger, connectionString, name)
        {
        }

        protected override IDbConnection CreateConnection(string dbName = "")
        {
            if (string.IsNullOrEmpty(dbName))
            {
                return new SqlConnection(connectionString);
            }
            var sb = new SqlConnectionStringBuilder(connectionString);
            sb.InitialCatalog = dbName;
            return new SqlConnection(sb.ConnectionString);
        }

        public override string GetDbName()
        {
            if (dbName == null)
            {
                var sb = new SqlConnectionStringBuilder(connectionString);
                dbName = sb.InitialCatalog;
            }

            return dbName;
        }

        public override bool IsMasterServer(string serverName = "")
        {
            const string sql = "select top 1 @@SERVERNAME from sys.tables";
            serverName = string.IsNullOrEmpty(serverName) ? Environment.MachineName : serverName;
            using (var connection = GetConnection("master"))
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
declare @status bit = 0;
select @status = 1
	from sys.databases
	where name = @name
		and user_access_desc = 'MULTI_USER'
		and state_desc = 'ONLINE'
select isnull(@status, 0)";

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
            return connectionString.Contains("Initial Catalog");
        }
    }
}