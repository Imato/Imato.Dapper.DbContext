namespace Imato.Dapper.DbContext
{
    public static class Postgres
    {
        public static string FromatTableName(string tableName)
        {
            return tableName.Contains(".") ? tableName : "public." + tableName;
        }
    }
}