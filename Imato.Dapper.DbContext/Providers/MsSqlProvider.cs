using Dapper;
using Imato.DumyMemoryCache;
using Imato.Reflection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Imato.Dapper.DbContext
{
    public class MsSqlProvider : BaseContextProvider, IContextProvider
    {
        private readonly DummyMemoryCache cache = new DummyMemoryCache();

        public MsSqlProvider(string? connectionString = null) : base(connectionString)
        {
        }

        public new ContextVendors Vendor => ContextVendors.mssql;

        public string FormatTableName(string tableName, string? schema = null)
        {
            return tableName.Contains(".") ? tableName : (schema ?? "dbo") + "." + tableName;
        }

        public override IDbConnection CreateConnection(string? connectionString = null)
        {
            return CreateConnection(ConnectionString ?? AppEnvironment.GetVariables(connectionString),
                "", "", "");
        }

        public string CreateConnectionString(string connectionString,
            string dataBase = "",
            string user = "",
            string password = "")
        {
            var key = $"{connectionString};{dataBase};{user};{password}";
            var replicaState = GetReplicaState(ref connectionString);
            var replicaStateTimeout = GetReplicaStateTimeout(ref connectionString);
            var sb = new SqlConnectionStringBuilder(AppEnvironment.GetVariables(connectionString));

            sb.InitialCatalog = !string.IsNullOrEmpty(dataBase) ? dataBase : sb.InitialCatalog;
            sb.UserID = !string.IsNullOrEmpty(user) ? user : sb.UserID;
            sb.Password = !string.IsNullOrEmpty(password) ? password : sb.Password;

            // for two and more replica. Data Source=srvd2695,srvd6201;
            if (sb.DataSource.Contains(","))
            {
                sb.ConnectionString = cache.Get(key,
                    replicaStateTimeout,
                    () =>
                    {
                        var hosts = sb.DataSource
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .ToDictionary(k => k, v => ReplicaState.Brocken);
                        foreach (var host in hosts.Keys)
                        {
                            sb.DataSource = host;
                            hosts[host] = GetReplicaStateAsync(sb.ConnectionString).Result;
                            if (hosts[host] == replicaState)
                            {
                                return sb.ConnectionString;
                            }
                        }

                        var sameHost = hosts.Where(x => x.Value >= replicaState)?.FirstOrDefault();
                        if (string.IsNullOrEmpty(sameHost?.Key))
                        {
                            throw new ApplicationException($"Cannot find replica state {replicaState} in hosts {string.Join(",", hosts.Keys)}");
                        }
                        sb.DataSource = sameHost?.Key;
                        return sb.ConnectionString;
                    });
            }
            return sb.ConnectionString;
        }

        public IDbConnection CreateConnection(string connectionString,
            string dataBase = "",
            string user = "",
            string password = "")
        {
            return new SqlConnection(CreateConnectionString(connectionString, dataBase, user, password));
        }

        public ReplicaState GetReplicaState(ref string connectionString)
        {
            connectionString = connectionString.PullParameter("ReplicaState", out var value);
            if (Enum.TryParse(typeof(ReplicaState), value, out var result))
            {
                return (ReplicaState)result;
            }
            return ReplicaState.ReadWrite;
        }

        public TimeSpan GetReplicaStateTimeout(ref string connectionString)
        {
            connectionString = connectionString.PullParameter("ReplicaStateTimeout", out var value);
            if (int.TryParse(value, out var result))
            {
                return TimeSpan.FromMilliseconds(result);
            }
            return TimeSpan.FromMilliseconds(60_000);
        }

        private async Task<ReplicaState> GetReplicaStateAsync(string connectionString)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                const string sql = @"
select top 1 e.value
	from sys.database_files f
		left join sys.extended_properties
			e on e.name = 'ReadOnly' and e.class = 0
";
                var result = await connection.QuerySingleAsync<string>(sql);
                if (result == "true")
                    return ReplicaState.ReadOnly;
                if (result == "false")
                    return ReplicaState.ReadWrite;
            }
            catch
            {
                return ReplicaState.Brocken;
            }
            return ReplicaState.ReadWrite;
        }

        public async Task<string?> FindTableAsync(
            IDbConnection connection,
            string tableName)
        {
            tableName = FormatTableName(tableName);
            var sql = $"select top 1 schema_name(schema_id) + '.' + name from sys.tables where object_id = object_id(@tableName)";
            return await connection.QuerySingleOrDefaultAsync<string>(sql, new { tableName });
        }

        public async Task<IEnumerable<TableColumn>> GetColumnsAsync(
            IDbConnection connection,
            string tableName)
        {
            tableName = FormatTableName(tableName);
            var sql = @"
select name, is_computed as isComputed, c.is_identity as isIdentity
from sys.columns c
where c.object_id = object_id(@tableName)
order by 1;";
            return await connection.QueryAsync<TableColumn>(sql, new { tableName });
        }

        private void Open(IDbConnection connection)
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }
        }

        private SqlBulkCopy BuildSqlBulkCopy<T>(IDbConnection connection,
            string tableName,
            IDictionary<string, string> mappings,
            int bulkCopyTimeoutSeconds,
            int batchSize)
        {
            var sqlConnection = (connection as SqlConnection)
                ?? throw new ApplicationException("Wrong connection type");
            Open(sqlConnection);
            var bulk = new SqlBulkCopy(sqlConnection);

            foreach (var m in mappings)
            {
                bulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping
                {
                    DestinationColumn = m.Value,
                    SourceColumn = m.Key
                });
            }
            bulk.DestinationTableName = tableName;
            bulk.BulkCopyTimeout = bulkCopyTimeoutSeconds;
            bulk.BatchSize = batchSize;

            return bulk;
        }

        public async Task BulkInsertAsync<T>(IDbConnection connection,
            IEnumerable<T> data,
            string? tableName = null,
            IEnumerable<string>? columns = null,
            int bulkCopyTimeoutSeconds = 30,
            int batchSize = 10000,
            bool skipFieldsCheck = false,
            ILogger? logger = null,
            IDictionary<string, string?>? mappings = null)
        {
            var rowCount = 0;
            tableName ??= TableAttributeExtensions.RequiredValue<T>();
            mappings ??= BulkCopy.GetMappingsOf<T>(columns, tableName, skipFieldsCheck, connection);
            logger?.LogDebug($"BulkInsert. Using mapping object -> column: {mappings.Serialize()}");

            using (var bulk = BuildSqlBulkCopy<T>(connection, tableName, mappings, bulkCopyTimeoutSeconds, batchSize))
            {
                var table = AddColumns(new DataTable(), mappings.Keys);

                var fields = mappings.Keys.ToArray();
                foreach (var r in data)
                {
                    var row = table.NewRow();
                    row = AddColumns(row, r, fields);
                    table.Rows.Add(row);
                    rowCount++;

                    if (rowCount > bulk.BatchSize)
                    {
                        logger?.LogDebug($"BulkInsert {rowCount} rows");
                        Open(connection);
                        await bulk.WriteToServerAsync(table);
                        table.Clear();
                        rowCount = 0;
                    }
                }

                if (rowCount > 0)
                {
                    logger?.LogDebug($"BulkInsert {rowCount} rows");
                    Open(connection);
                    await bulk.WriteToServerAsync(table);
                }
            }
        }

        private DataTable AddColumns(DataTable table, IEnumerable<string> fields)
        {
            foreach (var f in fields)
            {
                table.Columns.Add(new DataColumn
                {
                    ColumnName = f
                });
            }

            return table;
        }

        private DataRow AddColumns<T>(DataRow row, T data, string[] fields)
        {
            foreach (var v in Objects.GetFields(obj: data, fields: fields, skipChildren: true))
            {
                row[v.Key] = v.Value;
            }
            return row;
        }

        public override async Task<bool> IsReadWriteConnectionAsync(IDbConnection connection)
        {
            try
            {
                return await connection.QuerySingleOrDefaultAsync<bool>("select cast(1 as bit) as result");
            }
            catch { };
            return false;
        }
    }
}