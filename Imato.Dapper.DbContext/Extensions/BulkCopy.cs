using Imato.Reflection;
using Npgsql;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Imato.Dapper.DbContext
{
    public static class BulkCopy
    {
        private static ConcurrentDictionary<string, Dictionary<string, string>> _mapperColumns = new ConcurrentDictionary<string, Dictionary<string, string>>();

        public static IDictionary<string, string> GetColumnsOf<T>(IEnumerable<string>? columns = null)
        {
            var typeKey = typeof(T).Name
                + (columns != null ? ":" + string.Join(",", columns) : "");
            Dictionary<string, string> mapping;

            if (!_mapperColumns.TryGetValue(typeKey, out mapping))
            {
                mapping = columns != null
                    ? columns.ToDictionary(x => x.ToUpper())
                    : Objects.GetFieldNames<T>()
                        .ToDictionary(x => x.ToUpper());
                _mapperColumns.TryAdd(typeKey, mapping);
            }

            return mapping;
        }

        public static IDictionary<string, object?> GetValuesOf<T>(this T data,
            IEnumerable<string>? columns = null)
        {
            var result = Objects.GetFields(obj: data, skipChildren: true);
            if (columns == null)
            {
                return result;
            }

            foreach (var column in columns)
            {
                var key = result.ContainsKey(column)
                    ? column
                    : result
                        .Where(x => x.Key.ToUpper() == column.ToUpper())
                        .Select(x => x.Key)
                        .FirstOrDefault();

                if (key != null)
                {
                    var value = result[key];
                    result.Remove(key);
                    result.Add(column, value);
                }
            }

            return result;
        }

        public static async Task BulkInsertAsync<T>(this IDbConnection connection,
            IEnumerable<T> data,
            string? tableName = null,
            IEnumerable<string>? columns = null)
        {
            var ns = connection as NpgsqlConnection;
            if (ns != null)
            {
                await ns.BulkInsertAsync<T>(data, tableName, columns);
            }

            var ms = connection as SqlConnection;
            if (ms != null)
            {
                await ms.BulkInsertAsync<T>(data, tableName, columns);
            }
        }
    }
}