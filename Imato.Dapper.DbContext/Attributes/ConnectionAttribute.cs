using System;
using System.Linq;

namespace Imato.Dapper.DbContext
{
    public class ConnectionAttribute : Attribute
    {
        public string Name { get; private set; } = "";

        public ConnectionAttribute(string connectionName)
        {
            Name = connectionName;
        }

        public static string Value<T>(T obj)
        {
            return Value<T>();
        }

        public static string Value(Type t)
        {
            return t
                .GetCustomAttributes(false)
                .OfType<ConnectionAttribute>()
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
                : throw new ArgumentException($"Required Connection attribute for {typeof(T).Name}");
        }
    }
}