﻿using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Data;

using System.Threading.Tasks;

namespace Imato.Dapper.DbContext
{
    public interface IContextProvider
    {
        ContextVendors Vendor { get; }

        IDbConnection CreateConnection(string? connectionString = null);

        string CreateConnectionString(string connectionString,
            string dataBase = "",
            string user = "",
            string password = "");

        IDbConnection CreateConnection(string connectionString,
            string dataBase = "",
            string user = "",
            string password = "");

        string FormatTableName(string tableName, string? schema = null);

        /// <summary>
        /// Table columns
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        Task<IEnumerable<TableColumn>> GetColumnsAsync(
            IDbConnection connection,
            string tableName);

        Task BulkInsertAsync<T>(IDbConnection connection,
            IEnumerable<T> data,
            string? tableName = null,
            IEnumerable<string>? columns = null,
            int bulkCopyTimeoutSeconds = 30,
            int batchSize = 10000,
            bool skipFieldsCheck = false,
            ILogger? logger = null,
            IDictionary<string, string?>? mappings = null);

        /// <summary>
        /// Try find table by name
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        Task<string?> FindTableAsync(
            IDbConnection connection,
            string tableName);

        Task ExecuteAsync(string sql, int timeout = 3600);
    }
}