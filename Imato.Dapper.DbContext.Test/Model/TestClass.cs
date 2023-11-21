using Dapper.Contrib.Extensions;

namespace Imato.Dapper.DbContext.Test
{
    [Table("test_case_1")]
    public class TestClass
    {
        [ExplicitKey]
        public int id { get; set; }

        public string name { get; set; } = null!;
        public DateTime date { get; set; }
    }
}