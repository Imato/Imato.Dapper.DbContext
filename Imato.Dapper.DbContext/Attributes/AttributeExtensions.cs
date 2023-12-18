using System.ComponentModel.DataAnnotations.Schema;

namespace Imato.Dapper.DbContext
{
    public static class AttributeExtensions
    {
        public static string Db<T>(this T obj) where T : IDbObject
        {
            return DbAttribute.RequiredValue<T>();
        }

        public static string Connection<T>(this T obj) where T : IDbObject
        {
            return ConnectionAttribute.RequiredValue<T>();
        }

        public static string Table<T>(this T obj) where T : IDbObject
        {
            return TableAttributeExtensions.RequiredValue<T>();
        }
    }
}