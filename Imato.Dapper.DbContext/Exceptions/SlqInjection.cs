namespace Imato.Dapper.DbContext
{
    public static class SlqInjection
    {
        private static string[] exceptions =
        {
            "delete ",
            "drop ",
            "truncate ",
            "update ",
            "alter ",
            "insert ",
            "merge ",
            "create "
        };

        private static string[] exclude =
        {
            "insert into #",
            "create table #",
            "drop table #",
            "insert into @",
            "update @tmp",
            "update tmp"
         };

        public static string Check(string? sql)
        {
            if (string.IsNullOrEmpty(sql))
            {
                return sql;
            }

            var result = false;

            foreach (var ex in exceptions)
            {
                var position = sql.IndexOf(ex);
                if (position != -1)
                {
                    result = true;
                    foreach (var cl in exclude)
                    {
                        var maxPostion = position + cl.Length < sql.Length ? position + cl.Length : sql.Length - position;
                        if (sql.IndexOf(cl, position, maxPostion) != -1)
                        {
                            result = false;
                            break;
                        }
                    }

                    if (result)
                    {
                        throw new SqlInjectionException(sql, position);
                    }
                }
            }

            return sql;
        }
    }
}