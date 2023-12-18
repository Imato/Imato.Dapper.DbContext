using System.ComponentModel.DataAnnotations.Schema;

namespace Imato.Dapper.DbContext.Test
{
    [Db("unit_tests")]
    [Table("test_case_1")]
    public class TestClass
    {
        [ExplicitKey]
        public int id { get; set; }

        public string name { get; set; } = null!;
        public DateTime date { get; set; }
    }
}