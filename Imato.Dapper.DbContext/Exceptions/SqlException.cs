using System;

namespace Imato.Dapper.DbContext
{
    public class SqlException : ApplicationException
    {
        public SqlException(string sqlString) : base($"Exception in SQL string: {sqlString}")
        {
        }

        public SqlException(Exception inner, string sql, string? command)
            : base($"{inner.Message} in {(command != null && command != sql ? ("Command " + command) : "")} SQL: {sql}", inner)
        {
        }
    }
}