using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Imato.Dapper.DbContext
{
    public class MySql : IContextVendor
    {
        public ContextVendors Vendor => ContextVendors.mysql;

        public IDbConnection CreateConnection(string connectionString)
        {
            return new MySqlConnection(connectionString);
        }

        public IDbConnection CreateConnection(string connectionString,
            string dataBase = "",
            string user = "",
            string password = "")
        {
            var mb = new MySqlConnectionStringBuilder(connectionString);
            mb.Database = dataBase != "" ? dataBase : mb.Database;
            mb.UserID = string.IsNullOrEmpty(mb.UserID) ? user : mb.UserID;
            mb.Password = string.IsNullOrEmpty(mb.Password) ? password : mb.Password;
            return new MySqlConnection(mb.ConnectionString);
        }

        public Task BulkInsertAsync<T>(IDbConnection connection, IEnumerable<T> data, string? tableName = null, IEnumerable<string>? columns = null, int bulkCopyTimeoutSeconds = 30, int batchSize = 10000, bool skipFieldsCheck = false, ILogger? logger = null)
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

        public Task<string?> FindTableAsync(IDbConnection connection, string tableName)
        {
            throw new NotImplementedException();
        }
    }
}