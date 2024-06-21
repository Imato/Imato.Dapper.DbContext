using System;

namespace Imato.Dapper.DbContext
{
    public class SqlInjectionException : ApplicationException
    {
        public SqlInjectionException(string sqlString, int position)
            : base($"Injection in {position} into SQL string: \"{sqlString}\"")
        {
        }
    }
}