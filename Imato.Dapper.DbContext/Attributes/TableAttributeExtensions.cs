using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Imato.Dapper.DbContext
{
    public static class TableAttributeExtensions
    {
        public static string? Value<T>(T obj)
        {
            return Value<T>();
        }

        public static string? Value(Type t)
        {
            return t
                .GetCustomAttributes(false)
                .OfType<TableAttribute>()
                .FirstOrDefault()
                ?.Name;
        }

        public static string? Value<T>()
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