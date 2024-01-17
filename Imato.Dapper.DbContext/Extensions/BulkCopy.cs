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
        private static ConcurrentDictionary<string, Dictionary<string, string>> _mappings = new ConcurrentDictionary<string, Dictionary<string, string>>();

        /// <summary>
        /// Mapping field of T to table column
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="columns">table columns</param>
        /// <param name="tableName"></param>
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
            var fields = Objects.GetFieldNames<T>().ToArray();

            if (!_mappings.TryGetValue(typeKey, out mappings))
            {
                mappings = SqlMapperExtensions.MappingsOf(typeof(T))
                    ?? new Dictionary<string, string>();

                if (mappings.Count == 0 && !skipFieldsCheck)
                {
                    var cals = columns;
                    cals ??= connection != null ? GetColumnsAsync(connection, tableName).Result : Enumerable.Empty<string>();
                    var tableColumns = cals.ToDictionary(x => x.ToUpper());
                    foreach (var f in fields)
                    {
                        if (tableColumns.ContainsKey(f.ToUpper()))
                        {
                            mappings.Add(f, tableColumns[f.ToUpper()]);
                        }
                    }
                }

                if (mappings.Count == 0)
                {
                    var cs = columns?.ToArray() ?? fields;
                    for (int i = 0; i < cs.Length; i++)
                    {
                        mappings.Add(fields[i], cs[i]);
                    }
                }

                if (mappings.Count > 0)
                {
                    _mappings.TryAdd(typeKey, mappings);
                }
                else
                {
                    throw new ApplicationException($"Cannot generate mappings for table {tableName} with columns {string.Join(",", columns)} and type {typeof(T).Name}");
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
            bool skipFieldsCheck = false)
        {
            var ns = connection as NpgsqlConnection;
            if (ns != null)
            {
                return Postgres.BulkInsertAsync(ns, data, tableName, columns, skipFieldsCheck);
            }

            var ms = connection as SqlConnection;
            if (ms != null)
            {
                return MsSql.BulkInsertAsync(ms, data, tableName, columns, bulkCopyTimeoutSeconds, batchSize, skipFieldsCheck);
            }

            throw new NotImplementedException();
        }

        public static async Task<IEnumerable<string>> GetColumnsAsync(this IDbConnection connection,
            string tableName)
        {
            var ns = connection as NpgsqlConnection;
            if (ns != null)
            {
                return await Postgres.GetColumnsAsync(ns, tableName);
            }

            var ms = connection as SqlConnection;
            if (ms != null)
            {
                return await MsSql.GetColumnsAsync(ms, tableName);
            }

            throw new NotImplementedException();
        }
    }
}