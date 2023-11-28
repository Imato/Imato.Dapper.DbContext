using Dapper.Contrib.Extensions;
using System;
using System.Reflection;
using System.Text;

namespace Imato.Dapper.DbContext
{
    public static class Model
    {
        public static string GetTable<T>()
        {
            string? r = null;
            try
            {
                var ta = typeof(T)
                    .GetTypeInfo()
                    .GetCustomAttribute<global::Dapper.Contrib.Extensions.TableAttribute>();
                r = ta?.Name;
            }
            catch { }

            return r ?? throw new ApplicationException($"Att attribute Table to class {typeof(T).Name}");
        }
    }
}