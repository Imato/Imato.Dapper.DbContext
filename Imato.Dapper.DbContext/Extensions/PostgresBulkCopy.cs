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

            var mappings = SqlMapperExtensions.MappingsOf(typeof(T));

            if (mappings.Count == 0 && columns != null)
            {
                mappings = new Dictionary<string, string>();
                var fields = Objects.GetFieldNames<T>().ToArray();
                var cs = columns.ToArray();
                for (int i = 0; i < cs.Length; i++)
                {
                    mappings.Add(fields[i], cs[i]);
                }
            }

            if (mappings.Count == 0)
            {
                var tableColumns = (await connection.GetColumnsAsync(tableName))
                        .ToDictionary(x => x.ToUpper());
                mappings = new Dictionary<string, string>();
                foreach (var f in Objects.GetFieldNames<T>())
                {
                    if (tableColumns.ContainsKey(f.ToUpper()))
                    {
                        mappings.Add(f, tableColumns[f.ToUpper()]);
                    }
                }
            }

            var properties = mappings.Keys.ToArray();
            columns = mappings.Values.ToArray();

            using (var writer = connection.BeginBinaryImport($"copy {Postgres.FormatTableName(tableName)} ({string.Join(",", columns)}) from STDIN (FORMAT BINARY)"))
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

        public static async Task BulkInsertAsync<T>(this NpgsqlConnection connection,
            IEnumerable<T> data,
            string? tableName = null,
            IEnumerable<string>? columns = null)
        {
            await Write(connection, data, tableName, columns);
        }
    }
}