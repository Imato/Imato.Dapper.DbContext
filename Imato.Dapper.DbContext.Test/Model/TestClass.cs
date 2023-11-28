using Dapper.Contrib.Extensions;

namespace Imato.Dapper.DbContext.Test
{
    [Db("unit_tests")]
    [global::Dapper.Contrib.Extensions.Table("test_case_1")]
    public class TestClass
    {
        [ExplicitKey]
        public int id { get; set; }

        public string name { get; set; } = null!;
        public DateTime date { get; set; }
    }
}