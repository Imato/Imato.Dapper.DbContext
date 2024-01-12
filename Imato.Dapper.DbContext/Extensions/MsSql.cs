using Dapper;
using Imato.Reflection;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Imato.Dapper.DbContext
{
    public static class MsSql
    {
        public static string FormatTableName(string tableName)
        {
            return tableName.Contains(".") ? tableName : "dbo." + tableName;
        }

        public static async Task<IEnumerable<string>> GetColumnsAsync(SqlConnection connection,
            string tableName)
        {
            tableName = FormatTableName(tableName);
            var sql = $"select name from sys.columns c where c.object_id = object_id(@tableName) and c.is_computed = 0 order by 1;";
            return await connection.QueryAsync<string>(sql, new { tableName });
        }

        private static SqlBulkCopy BuildSqlBulkCopy<T>(SqlConnection connection,
            string tableName,
            IDictionary<string, string> mappings,
            int bulkCopyTimeoutSeconds,
            int batchSize)
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }
            var bulk = new SqlBulkCopy(connection);

            foreach (var m in mappings)
            {
                bulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping
                {
                    DestinationColumn = m.Value,
                    SourceColumn = m.Key
                });
            }
            bulk.DestinationTableName = tableName;
            bulk.BulkCopyTimeout = bulkCopyTimeoutSeconds;
            bulk.BatchSize = batchSize;

            return bulk;
        }

        public static async Task BulkInsertAsync<T>(this SqlConnection connection,
            IEnumerable<T> data,
            string? tableName = null,
            IEnumerable<string>? columns = null,
            int bulkCopyTimeoutSeconds = 30,
            int batchSize = 10000,
            bool skipFieldsCheck = false)
        {
            var rowCount = 0;
            tableName ??= TableAttributeExtensions.RequiredValue<T>();
            var mappings = BulkCopy.GetMappingsOf<T>(columns, tableName, skipFieldsCheck, connection);

            using (var bulk = BuildSqlBulkCopy<T>(connection, tableName, mappings, bulkCopyTimeoutSeconds, batchSize))
            {
                var table = new DataTable()
                    .AddColumns<T>(mappings.Keys);

                var fields = mappings.Keys.ToArray();
                foreach (var r in data)
                {
                    var row = table.NewRow();
                    row.AddColumns(r, fields);
                    table.Rows.Add(row);
                    rowCount++;

                    if (rowCount > bulk.BatchSize)
                    {
                        await bulk.WriteToServerAsync(table);
                        table.Clear();
                        rowCount = 0;
                    }
                }

                if (rowCount > 0)
                {
                    await bulk.WriteToServerAsync(table);
                }
            }
        }

        private static DataTable AddColumns<T>(this DataTable table, IEnumerable<string> fields)
        {
            foreach (var f in fields)
            {
                table.Columns.Add(new DataColumn
                {
                    ColumnName = f
                });
            }

            return table;
        }

        private static DataRow AddColumns<T>(this DataRow row, T data, string[] fields)
        {
            foreach (var v in Objects.GetFields(obj: data, fields: fields, skipChildren: true))
            {
                row[v.Key] = v.Value;
            }
            return row;
        }
    }
}