using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Imato.Dapper.DbContext
{
    public static class Postgres
    {
        public static string FormatTableName(string tableName)
        {
            return tableName.Contains(".") ? tableName : "public." + tableName;
        }

        public static async Task<IEnumerable<string>> GetColumnsAsync(this IDbConnection connection,
            string tableName)
        {
            var sql = $"select column_name from information_schema.columns where table_schema ||  '.' || table_name = @tableName and is_generated = 'NEVER' and is_updatable = 'YES' order by 1;";
            return await connection.QueryAsync<string>(sql, new { tableName });
        }
    }
}