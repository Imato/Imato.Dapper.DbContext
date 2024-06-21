using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Imato.Dapper.DbContext
{
    public static class MsSqlExtensions
    {
        private static MsSqlProvider msSql = new MsSqlProvider();

        public static string FormatTableName(string tableName)
        {
            return tableName.Contains(".") ? tableName : "dbo." + tableName;
        }

        public static Task<IEnumerable<string>> GetColumnsAsync(this SqlConnection connection,
            string tableName)
        {
            return msSql.GetColumnsAsync(connection, tableName);
        }

        public static Task BulkInsertAsync<T>(this SqlConnection connection,
            IEnumerable<T> data,
            string? tableName = null,
            IEnumerable<string>? columns = null,
            int bulkCopyTimeoutSeconds = 30,
            int batchSize = 10000,
            bool skipFieldsCheck = false)
        {
            return msSql.BulkInsertAsync(connection, data, tableName, columns, bulkCopyTimeoutSeconds, batchSize, skipFieldsCheck);
        }
    }
}