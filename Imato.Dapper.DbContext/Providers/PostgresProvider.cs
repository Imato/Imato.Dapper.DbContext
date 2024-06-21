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

        public ContextVendors Vendor => ContextVendors.postgres;

        public string FormatTableName(string tableName, string? schema = null)
        {
            return tableName.Contains(".") ? tableName : (schema ?? "public") + "." + tableName;
        }

        public override IDbConnection CreateConnection(string? connectionString = null)
        {
            return new NpgsqlConnection(ConnectionString ?? connectionString);
        }

        public IDbConnection CreateConnection(string connectionString,
            string dataBase = "",
            string user = "",
            string password = "")
        {
            var nb = new NpgsqlConnectionStringBuilder(connectionString);
            nb.Database = dataBase != "" ? dataBase : nb.Database;
            nb.Username = string.IsNullOrEmpty(nb.Username) ? user : nb.Username;
            nb.Password = string.IsNullOrEmpty(nb.Password) ? password : nb.Password;
            return new NpgsqlConnection(nb.ConnectionString);
        }

        public async Task<string?> FindTableAsync(
            IDbConnection connection,
            string tableName)
        {
            tableName = FormatTableName(tableName);
            var sql = $"select table_schema ||  '.' || table_name from information_schema.tables where table_schema ||  '.' || table_name = @tableName limit 1;";
            return await connection.QuerySingleOrDefaultAsync<string>(sql, new { tableName });
        }

        public Task<IEnumerable<string>> GetColumnsAsync(IDbConnection connection,
            string tableName)
        {
            tableName = FormatTableName(tableName);
            var sql = $"select column_name from information_schema.columns where table_schema ||  '.' || table_name = @tableName and is_generated = 'NEVER' and is_updatable = 'YES' order by 1;";
            return connection.QueryAsync<string>(sql, new { tableName });
        }

        public Task BulkInsertAsync<T>(IDbConnection connection,
            IEnumerable<T> data,
            string? tableName = null,
            IEnumerable<string>? columns = null,
            int bulkCopyTimeoutSeconds = 30,
            int batchSize = 10000,
            bool skipFieldsCheck = false,
            ILogger? logger = null)
        {
            var pgConnection = (connection as NpgsqlConnection)
                ?? throw new ApplicationException("Wrong connection type");

            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }
            tableName ??= TableAttributeExtensions.RequiredValue<T>();
            tableName = PostgresExtensions.FormatTableName(tableName);

            var mappings = BulkCopy.GetMappingsOf<T>(columns, tableName, skipFieldsCheck, connection);
            logger?.LogDebug($"BulkInsert. Using mapping object -> column: {mappings.Serialize()}");
            var properties = mappings.Keys.ToArray();
            columns = mappings.Values.ToArray();

            using (var writer = pgConnection.BeginBinaryImport($"copy {PostgresExtensions.FormatTableName(tableName)} ({string.Join(",", columns)}) from STDIN (FORMAT BINARY)"))
            {
                writer.Timeout = TimeSpan.FromSeconds(bulkCopyTimeoutSeconds);
                foreach (var d in data)
                {
                    writer.StartRow();
                    foreach (var p in properties)
                    {
                        var value = Objects.GetField(d, p);
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

                                case nameof(Int16):
                                    writer.Write(value, NpgsqlDbType.Smallint);
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
                }

                writer.Complete();
            }

            return Task.CompletedTask;
        }
    }
}