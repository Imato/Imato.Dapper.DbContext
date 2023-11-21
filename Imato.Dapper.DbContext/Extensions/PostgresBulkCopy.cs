using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
            tableName ??= Model.GetTable<T>() ?? throw new ArgumentException($"Empty parameter {nameof(tableName)}");
            tableName = Postgres.FromatTableName(tableName);
            var columnNames = string.Join(",", columns ?? BulkCopy.GetColumnsOf<T>().Values);
            using (var writer = connection.BeginBinaryImport($"copy {Postgres.FromatTableName(tableName)} ({columnNames}) from STDIN (FORMAT BINARY)"))
            {
                foreach (var d in data)
                {
                    writer.StartRow();
                    foreach (var v in BulkCopy.GetValuesOf(d, columns))
                    {
                        await writer.WriteAsync(v.Value);
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