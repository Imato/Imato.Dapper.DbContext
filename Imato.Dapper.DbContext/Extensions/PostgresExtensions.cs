using Dapper;
using Imato.Reflection;
using Npgsql;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Imato.Dapper.DbContext
{
    public static class PostgresExtensions
    {
        public static string FormatTableName(string tableName)
        {
            return tableName.Contains(".") ? tableName : "public." + tableName;
        }

        public static async Task<IEnumerable<string>> GetColumnsAsync(NpgsqlConnection connection,
            string tableName)
        {
            tableName = FormatTableName(tableName);
            var sql = $"select column_name from information_schema.columns where table_schema ||  '.' || table_name = @tableName and is_generated = 'NEVER' and is_updatable = 'YES' order by 1;";
            return await connection.QueryAsync<string>(sql, new { tableName });
        }

        public static async Task BulkInsertAsync<T>(NpgsqlConnection connection,
            IEnumerable<T> data,
            string? tableName = null,
            IEnumerable<string>? columns = null,
            bool skipFieldsCheck = false)
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }
            tableName ??= TableAttributeExtensions.RequiredValue<T>();
            tableName = PostgresExtensions.FormatTableName(tableName);

            var mappings = BulkCopy.GetMappingsOf<T>(columns, tableName, skipFieldsCheck, connection);
            var properties = mappings.Keys.ToArray();
            columns = mappings.Values.ToArray();

            using (var writer = connection.BeginBinaryImport($"copy {PostgresExtensions.FormatTableName(tableName)} ({string.Join(",", columns)}) from STDIN (FORMAT BINARY)"))
            {
                foreach (var d in data)
                {
                    writer.StartRow();
                    foreach (var p in properties)
                    {
                        var v = Objects.GetField(d, p);
                        await writer.WriteAsync(v);
                    }
                }

                writer.Complete();
            }
        }
    }
}