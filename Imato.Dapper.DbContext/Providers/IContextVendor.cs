using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Data;

using System.Threading.Tasks;

namespace Imato.Dapper.DbContext
{
    public interface IContextVendor
    {
        ContextVendors Vendor { get; }

        string FormatTableName(string tableName, string? schema = null);

        Task<IEnumerable<string>> GetColumnsAsync(
            IDbConnection connection,
            string tableName);

        Task BulkInsertAsync<T>(IDbConnection connection,
            IEnumerable<T> data,
            string? tableName = null,
            IEnumerable<string>? columns = null,
            int bulkCopyTimeoutSeconds = 30,
            int batchSize = 10000,
            bool skipFieldsCheck = false,
            ILogger? logger = null);
    }
}