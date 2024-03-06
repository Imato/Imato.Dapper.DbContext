using Dapper;
using Imato.Reflection;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Imato.Dapper.DbContext
{
    public class Postgres : IContextVendor
    {
        public ContextVendors Vendor => ContextVendors.postgres;

        public string FormatTableName(string tableName, string? schema = null)
        {
            return tableName.Contains(".") ? tableName : (schema ?? "public") + "." + tableName;
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
                        var v = Objects.GetField(d, p);
                        writer.Write(v);
                    }
                }

                writer.Complete();
            }

            return Task.CompletedTask;
        }
    }
}