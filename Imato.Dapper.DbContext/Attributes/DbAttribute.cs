using System.Linq;

namespace System.ComponentModel.DataAnnotations.Schema
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class DbAttribute : Attribute
    {
        public string Name { get; private set; } = "";

        public DbAttribute(string dbName)
        {
            Name = dbName;
        }

        public static string Value<T>(T obj)
        {
            return Value<T>();
        }

        public static string Value(Type t)
        {
            return t
                .GetCustomAttributes(false)
                .OfType<DbAttribute>()
                .FirstOrDefault()
                ?.Name
                ?? ""; ;
        }

        public static string Value<T>()
        {
            return Value(typeof(T));
        }

        public static string RequiredValue<T>()
        {
            var v = Value(typeof(T));
            return !string.IsNullOrEmpty(v)
                ? v
                : throw new ArgumentException($"Required Table attribute for {typeof(T).Name}");
        }
    }
}