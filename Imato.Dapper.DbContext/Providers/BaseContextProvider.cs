using Dapper;
using System;
using System.Data;
using System.Threading.Tasks;

namespace Imato.Dapper.DbContext
{
    public class BaseContextProvider
    {
        protected readonly string? ConnectionString;

        public BaseContextProvider(string? connectionString = null)
        {
            ConnectionString = connectionString;
        }

        public ContextVendors Vendor => ContextVendors.mssql;

        public string FormatTableName(string tableName, string? schema = null)
        {
            return tableName.Contains(".") ? tableName : (schema ?? "dbo") + "." + tableName;
        }

        public virtual IDbConnection CreateConnection(string? connectionString = null)
        {
            throw new NotImplementedException();
        }

        public Task ExecuteAsync(string sql, int timeout = 3600)
        {
            using var c = CreateConnection();
            return c.ExecuteAsync(sql: sql, commandTimeout: timeout);
        }
    }
}