using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Imato.Dapper.DbContext
{
    public class MySql : IContextVendor
    {
        public ContextVendors Vendor => ContextVendors.mysql;

        public Task BulkInsertAsync<T>(IDbConnection connection, IEnumerable<T> data, string? tableName = null, IEnumerable<string>? columns = null, int bulkCopyTimeoutSeconds = 30, int batchSize = 10000, bool skipFieldsCheck = false)
        {
            throw new NotImplementedException();
        }

        public string FormatTableName(string tableName, string? schema = null)
        {
            return tableName;
        }

        public Task<IEnumerable<string>> GetColumnsAsync(IDbConnection connection, string tableName)
        {
            throw new NotImplementedException();
        }
    }
}