using Dapper;
using Imato.Reflection;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Imato.Dapper.DbContext
{
    public class PostgresProvider : BaseContextProvider, IContextProvider
    {
        public PostgresProvider(string? connectionString = null) : base(connectionString)
        {
        }

        public new ContextVendors Vendor => ContextVendors.postgres;

        public string FormatTableName(string tableName, string? schema = null)
        {
            return tableName.Contains(".") ? tableName : (schema ?? "public") + "." + tableName;
        }

        public override IDbConnection CreateConnection(string? connectionString = null)
        {
            return CreateConnection(ConnectionString ?? AppEnvironment.GetVariables(connectionString), "", "", "");
        }

        public string CreateConnectionString(string connectionString,
            string dataBase = "",
            string user = "",
            string password = "")
        {
            var nb = new NpgsqlConnectionStringBuilder(AppEnvironment.GetVariables(connectionString));
            nb.Database = !string.IsNullOrEmpty(dataBase) ? dataBase : nb.Database;
            nb.Username = !string.IsNullOrEmpty(user) ? user : nb.Username;
            nb.Password = !string.IsNullOrEmpty(password) ? password : nb.Password;
            return nb.ConnectionString;
        }

        public IDbConnection CreateConnection(string connectionString,
            string dataBase = "",
            string user = "",
            string password = "")
        {
            return new NpgsqlConnection(CreateConnectionString(connectionString, dataBase, user, password));
        }

        public async Task<string?> FindTableAsync(
            IDbConnection connection,
            string tableName)
        {
            tableName = FormatTableName(tableName);
            var sql = $"select table_schema ||  '.' || table_name from information_schema.tables where table_schema ||  '.' || table_name = @tableName limit 1;";
            return await connection.QuerySingleOrDefaultAsync<string>(sql, new { tableName = FormatTableName(tableName) });
        }

        public async Task<IEnumerable<TableColumn>> GetColumnsAsync(IDbConnection connection,
            string tableName)
        {
            tableName = FormatTableName(tableName);
            var sql = @"
select column_name as name,
	iif(is_updatable = 'YES', false, true) as isComputed,
	iif(is_generated = 'ALWAYS' or column_default like 'nextval(%', true, false) as isIdentity
from information_schema.columns
where table_schema ||  '.' || table_name = @tableName
order by 1;";
            return await connection.QueryAsync<TableColumn>(sql, new { tableName });
        }

        public Task BulkInsertAsync<T>(IDbConnection connection,
            IEnumerable<T> data,
            string? tableName = null,
            IEnumerable<string>? columns = null,
            int bulkCopyTimeoutSeconds = 30,
            int batchSize = 10000,
            bool skipFieldsCheck = false,
            ILogger? logger = null,
            IDictionary<string, string?>? mappings = null)
        {
            var pgConnection = (connection as NpgsqlConnection)
                ?? throw new ApplicationException("Wrong connection type");

            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }
            tableName ??= TableAttributeExtensions.RequiredValue<T>();
            tableName = FormatTableName(tableName);

            mappings ??= BulkCopy.GetMappingsOf<T>(columns, tableName, skipFieldsCheck, connection);
            logger?.LogDebug($"BulkInsert. Using mapping object -> column: {mappings?.Serialize()}");
            columns = mappings?.Values?.ToArray();

            using (var writer = pgConnection.BeginBinaryImport($"copy {FormatTableName(tableName)} ({string.Join(",", columns)}) from STDIN (FORMAT BINARY)"))
            {
                writer.Timeout = TimeSpan.FromSeconds(bulkCopyTimeoutSeconds);
                foreach (var d in data)
                {
                    writer.StartRow();
                    foreach (var m in mappings!)
                    {
                        var value = Objects.GetField(d, m.Key);
                        try
                        {
                            if (value != null)
                            {
                                switch (value.GetType().Name)
                                {
                                    case nameof(DateTime):
                                        var date = (DateTime)value;
                                        if (date.Kind == DateTimeKind.Utc)
                                        {
                                            writer.Write(date, NpgsqlDbType.TimestampTz);
                                        }
                                        else
                                        {
                                            writer.Write(date, NpgsqlDbType.Timestamp);
                                        }
                                        break;

                                    case nameof(Boolean):
                                        writer.Write(value, NpgsqlDbType.Boolean);
                                        break;

                                    case nameof(Byte):
                                    case nameof(Int16):
                                        writer.Write(value, NpgsqlDbType.Smallint);
                                        break;

                                    case nameof(Int32):
                                        writer.Write(value, NpgsqlDbType.Integer);
                                        break;

                                    case nameof(Int64):
                                        writer.Write(value, NpgsqlDbType.Bigint);
                                        break;

                                    case nameof(String):
                                        writer.Write(value.ToString());
                                        break;

                                    default:
                                        writer.Write(value);
                                        break;
                                }
                            }
                            else
                            {
                                writer.WriteNull();
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new ApplicationException($"Cannot write value {value} type {value?.GetType()?.Name ?? "null"} to column {m.Value}", ex);
                        }
                    }
                }

                writer.Complete();
            }

            return Task.CompletedTask;
        }

        public override async Task<bool> IsReadWriteConnectionAsync(IDbConnection connection)
        {
            try
            {
                return await connection.QuerySingleOrDefaultAsync<bool>("select pg_is_in_recovery() = false");
            }
            catch { };
            return false;
        }
    }
}