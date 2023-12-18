namespace Imato.Dapper.DbContext
{
    public static class MsSql
    {
        public static string FormatTableName(string tableName)
        {
            return tableName.Contains(".") ? tableName : "dbo." + tableName;
        }
    }
}