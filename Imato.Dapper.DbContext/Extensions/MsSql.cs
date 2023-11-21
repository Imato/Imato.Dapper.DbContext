namespace Imato.Dapper.DbContext
{
    public static class MsSql
    {
        public static string FromatTableName(string tableName)
        {
            return tableName.Contains(".") ? tableName : "dbo." + tableName;
        }
    }
}