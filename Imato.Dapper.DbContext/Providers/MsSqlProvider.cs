using Dapper;
using Imato.Reflection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Imato.Dapper.DbContext
{
    public class MsSqlProvider : BaseContextProvider, IContextProvider
    {
        public MsSqlProvider(string? connectionString = null) : base(connectionString)
        {
        }

        public ContextVendors Vendor => ContextVendors.mssql;

        public string FormatTableName(string tableName, string? schema = null)
        {
            return tableName.Contains(".") ? tableName : (schema ?? "dbo") + "." + tableName;
        }

        public override IDbConnection CreateConnection(string? connectionString = null)
        {
            return new SqlConnection(ConnectionString ?? connectionString);
        }

        public IDbConnection CreateConnection(string connectionString,
            string dataBase = "",
            string user = "",
            string password = "")
        {
            var sb = new SqlConnectionStringBuilder(connectionString);
            sb.InitialCatalog = dataBase != "" ? dataBase : sb.InitialCatalog;
            sb.UserID = string.IsNullOrEmpty(sb.UserID) ? user : sb.UserID;
            sb.Password = string.IsNullOrEmpty(sb.Password) ? password : sb.Password;
            return new SqlConnection(sb.ConnectionString);
        }

        public async Task<string?> FindTableAsync(
            IDbConnection connection,
            string tableName)
        {
            tableName = FormatTableName(tableName);
            var sql = $"select top 1 schema_name(schema_id) + '.' + name from sys.tables where object_id = object_id(@tableName)";
            return await connection.QuerySingleOrDefaultAsync<string>(sql, new { tableName });
        }

        public async Task<IEnumerable<string>> GetColumnsAsync(
            IDbConnection connection,
            string tableName)
        {
            tableName = FormatTableName(tableName);
            var sql = $"select name from sys.columns c where c.object_id = object_id(@tableName) and c.is_computed = 0 order by 1;";
            return await connection.QueryAsync<string>(sql, new { tableName });
        }

        private void Open(IDbConnection connection)
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }
        }

        private SqlBulkCopy BuildSqlBulkCopy<T>(IDbConnection connection,
            string tableName,
            IDictionary<string, string> mappings,
            int bulkCopyTimeoutSeconds,
            int batchSize)
        {
            var sqlConnection = (connection as SqlConnection)
                ?? throw new ApplicationException("Wrong connection type");
            Open(sqlConnection);
            var bulk = new SqlBulkCopy(sqlConnection);

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

        public async Task BulkInsertAsync<T>(IDbConnection connection,
            IEnumerable<T> data,
            string? tableName = null,
            IEnumerable<string>? columns = null,
            int bulkCopyTimeoutSeconds = 30,
            int batchSize = 10000,
            bool skipFieldsCheck = false,
            ILogger? logger = null)
        {
            var rowCount = 0;
            tableName ??= TableAttributeExtensions.RequiredValue<T>();
            var mappings = BulkCopy.GetMappingsOf<T>(columns, tableName, skipFieldsCheck, connection);
            logger?.LogDebug($"BulkInsert. Using mapping object -> column: {mappings.Serialize()}");

            using (var bulk = BuildSqlBulkCopy<T>(connection, tableName, mappings, bulkCopyTimeoutSeconds, batchSize))
            {
                var table = AddColumns(new DataTable(), mappings.Keys);

                var fields = mappings.Keys.ToArray();
                foreach (var r in data)
                {
                    var row = table.NewRow();
                    row = AddColumns(row, r, fields);
                    table.Rows.Add(row);
                    rowCount++;

                    if (rowCount > bulk.BatchSize)
                    {
                        logger?.LogDebug($"BulkInsert {rowCount} rows");
                        Open(connection);
                        await bulk.WriteToServerAsync(table);
                        table.Clear();
                        rowCount = 0;
                    }
                }

                if (rowCount > 0)
                {
                    logger?.LogDebug($"BulkInsert {rowCount} rows");
                    Open(connection);
                    await bulk.WriteToServerAsync(table);
                }
            }
        }

        private DataTable AddColumns(DataTable table, IEnumerable<string> fields)
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

        private DataRow AddColumns<T>(DataRow row, T data, string[] fields)
        {
            foreach (var v in Objects.GetFields(obj: data, fields: fields, skipChildren: true))
            {
                row[v.Key] = v.Value;
            }
            return row;
        }
    }
}