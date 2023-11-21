using Npgsql;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Imato.Dapper.DbContext
{
    public static class DbExtensions
    {
        public static DbConnectionStringBuilder AddUserPassword(
           this DbConnectionStringBuilder builder,
           string? user = null,
           string? password = null)
        {
            builder.AddValue("User ID", user);
            builder.AddValue("Password", password);
            return builder;
        }

        public static DbConnectionStringBuilder AddValue(
           this DbConnectionStringBuilder builder,
           string? key = null,
           object? value = null)
        {
            if (!string.IsNullOrEmpty(key) && value != null)
            {
                builder.Add(key, value);
            }
            return builder;
        }
    }
}