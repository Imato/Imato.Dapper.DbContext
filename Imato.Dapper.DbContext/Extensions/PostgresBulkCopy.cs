using Npgsql;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Imato.Reflection;

namespace Imato.Dapper.DbContext
{
    public static class PostgresBulkCopy
    {
        private static async Task Write<T>(NpgsqlConnection connection,
            IEnumerable<T> data,
            string? tableName = null,
            IEnumerable<string>? columns = null)
        {
            connection.Open();
            tableName ??= TableAttributeExtensions.RequiredValue<T>();
            tableName = Postgres.FormatTableName(tableName);

            if (columns == null)
            {
                var tableColumns = (await connection.GetColumnsAsync(tableName))
                    .ToDictionary(x => x.ToUpper());
                columns = BulkCopy.GetColumnsOf<T>()
                    .Where(x => tableColumns.ContainsKey(x.Key))
                    .Select(x => tableColumns[x.Key]);
            }

            using (var writer = connection.BeginBinaryImport($"copy {Postgres.FormatTableName(tableName)} ({string.Join(",", columns)}) from STDIN (FORMAT BINARY)"))
            {
                foreach (var d in data)
                {
                    writer.StartRow();
                    foreach (var c in columns)
                    {
                        var v = Objects.GetField(d, c);
                        await writer.WriteAsync(v);
                    }
                }

                writer.Complete();
            }
        }

        public static async Task BulkInsertAsync<T>(this NpgsqlConnection connection,
            IEnumerable<T> data,
            string? tableName = null,
            IEnumerable<string>? columns = null)
        {
            await Write(connection, data, tableName, columns);
        }
    }
}