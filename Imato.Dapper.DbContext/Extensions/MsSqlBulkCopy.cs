using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Imato.Dapper.DbContext
{
    public static class MsSqlBulkCopy
    {
        private static SqlBulkCopy BuildSqlBulkCopy<T>(SqlConnection connection,
            string? tableName = null,
            IEnumerable<string>? columns = null,
            int bulkCopyTimeoutSeconds = 30,
            int batchSize = 10000)
        {
            connection.Open();
            var bulk = new SqlBulkCopy(connection)
                .AddMappings<T>(columns);
            bulk.DestinationTableName = tableName ?? TableAttributeExtensions.RequiredValue<T>();
            bulk.BulkCopyTimeout = bulkCopyTimeoutSeconds;
            bulk.BatchSize = batchSize;

            return bulk;
        }

        public static void BulkInsert<T>(this SqlConnection connection,
            IEnumerable<T> data,
            string? tableName = null,
            IEnumerable<string>? columns = null,
            int bulkCopyTimeoutSeconds = 30,
            int batchSize = 10000)
        {
            var rowCount = 0;

            using (var bulk = BuildSqlBulkCopy<T>(connection, tableName, columns, bulkCopyTimeoutSeconds, batchSize))
            {
                var table = new DataTable()
                    .AddColumns<T>(columns);

                foreach (var r in data)
                {
                    var row = table.NewRow();
                    row.AddColumns(r, columns);
                    table.Rows.Add(row);
                    rowCount++;

                    if (rowCount > bulk.BatchSize)
                    {
                        bulk.WriteToServer(table);
                        table.Clear();
                        rowCount = 0;
                    }
                }

                if (rowCount > 0)
                {
                    bulk.WriteToServer(table);
                }
            }
        }

        public static async Task BulkInsertAsync<T>(this SqlConnection connection,
            IEnumerable<T> data,
            string? tableName = null,
            IEnumerable<string>? columns = null,
            int bulkCopyTimeoutSeconds = 30,
            int batchSize = 10000)
        {
            var rowCount = 0;

            using (var bulk = BuildSqlBulkCopy<T>(connection, tableName, columns, bulkCopyTimeoutSeconds, batchSize))
            {
                var table = new DataTable()
                    .AddColumns<T>(columns);

                foreach (var r in data)
                {
                    var row = table.NewRow();
                    row.AddColumns(r, columns);
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

        private static SqlBulkCopy AddMappings<T>(this SqlBulkCopy bulk, IEnumerable<string>? columns = null)
        {
            columns ??= BulkCopy.GetColumnsOf<T>().Values;
            foreach (var c in columns)
            {
                bulk.ColumnMappings.Add(new SqlBulkCopyColumnMapping
                {
                    DestinationColumn = c,
                    SourceColumn = c
                });
            }

            return bulk;
        }

        private static DataTable AddColumns<T>(this DataTable table, IEnumerable<string>? columns = null)
        {
            columns ??= BulkCopy.GetColumnsOf<T>().Values;
            foreach (var c in columns)
            {
                table.Columns.Add(new DataColumn
                {
                    ColumnName = c
                });
            }

            return table;
        }

        private static DataRow AddColumns<T>(this DataRow row, T data, IEnumerable<string>? columns = null)
        {
            foreach (var v in BulkCopy.GetValuesOf<T>(data, columns))
            {
                row[v.Key] = v.Value;
            }
            return row;
        }
    }
}