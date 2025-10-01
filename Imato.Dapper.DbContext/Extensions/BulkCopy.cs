using Imato.Reflection;
using Npgsql;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Imato.Dapper.DbContext
{
    public static class BulkCopy
    {
        private static ConcurrentDictionary<string, Dictionary<string, string>> _mappings = new ConcurrentDictionary<string, Dictionary<string, string>>();
        private static PostgresProvider postgresProvider = new PostgresProvider();
        private static MsSqlProvider msSqlProvider = new MsSqlProvider();

        /// <summary>
        /// Mapping field of T to table column
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="columns">table columns</param>
        /// <param name="skipFieldsCheck">Don`t find columns from list in type T, use all fields in T</param>
        /// <returns></returns>
        public static IDictionary<string, string?> GetMappingsOf<T>
            (IEnumerable<string>? columns = null,
            string? tableName = null,
            bool skipFieldsCheck = false,
            IDbConnection? connection = null)
        {
            var typeKey = typeof(T).Name
                + (columns != null ? ":" + string.Join(",", columns) : "");
            Dictionary<string, string>? mappings;

            tableName ??= TableAttributeExtensions.RequiredValue<T>();

            if (!_mappings.TryGetValue(typeKey, out mappings))
            {
                mappings = SqlMapperExtensions.MappingsOf<T>();

                var tableColumns = !skipFieldsCheck && connection != null ?
                    GetColumnsAsync(connection, tableName).Result
                        .Where(x => !x.IsIdentity && !x.IsComputed)
                        .Select(x => x.Name)
                        .ToDictionary(x => x.ToUpper())
                    : null;

                foreach (var k in mappings.Keys)
                {
                    if (tableColumns != null
                        && mappings.ContainsKey(k)
                        && !tableColumns.ContainsKey(mappings[k].ToUpper()))
                    {
                        mappings.Remove(k);
                    }
                    if (columns != null
                        && mappings.ContainsKey(k)
                        && columns.Any()
                        && !columns.Any(x => string.Equals(x, mappings[k], StringComparison.OrdinalIgnoreCase)))
                    {
                        mappings.Remove(k);
                    }
                    if (tableColumns != null
                        && mappings.ContainsKey(k)
                        && tableColumns.ContainsKey(mappings[k].ToUpper())
                        && mappings[k] != tableColumns[mappings[k].ToUpper()])
                    {
                        mappings[k] = tableColumns[mappings[k].ToUpper()];
                    }
                }

                if (mappings.Count > 0)
                {
                    _mappings.TryAdd(typeKey, mappings);
                }
                else
                {
                    throw new ApplicationException($"Cannot generate mappings for table {tableName} with columns {(columns == null ? "null" : string.Join(",", columns))} and type {typeof(T).Name}");
                }
            }

            return mappings!;
        }

        /// <summary>
        /// Bulk insert data into table
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="data"></param>
        /// <param name="tableName">Table name or [Table] attribute in type T</param>
        /// <param name="columns">Table columns list</param>
        /// <param name="skipFieldsCheck">Don`t find columns from list in type T, use all fields in T</param>
        /// <returns></returns>
        public static Task BulkInsertAsync<T>(this IDbConnection connection,
            IEnumerable<T> data,
            string? tableName = null,
            IEnumerable<string>? columns = null,
            int bulkCopyTimeoutSeconds = 30,
            int batchSize = 1_000,
            bool skipFieldsCheck = false,
            IDictionary<string, string?>? mappings = null)
        {
            var ns = connection as NpgsqlConnection;
            if (ns != null)
            {
                return postgresProvider.BulkInsertAsync(ns, data, tableName, columns, bulkCopyTimeoutSeconds, batchSize, skipFieldsCheck, null, mappings);
            }

            var ms = connection as SqlConnection;
            if (ms != null)
            {
                return msSqlProvider.BulkInsertAsync(ms, data, tableName, columns, bulkCopyTimeoutSeconds, batchSize, skipFieldsCheck, null, mappings);
            }

            throw new NotImplementedException();
        }

        public static async Task<IEnumerable<TableColumn>> GetColumnsAsync(this IDbConnection connection,
            string tableName)
        {
            var ns = connection as NpgsqlConnection;
            if (ns != null)
            {
                return await postgresProvider.GetColumnsAsync(ns, tableName);
            }

            var ms = connection as SqlConnection;
            if (ms != null)
            {
                return await msSqlProvider.GetColumnsAsync(ms, tableName);
            }

            throw new NotImplementedException();
        }
    }
}