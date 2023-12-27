using System.ComponentModel.DataAnnotations.Schema;

namespace Imato.Dapper.DbContext.Test
{
    [Connection("mssql")]
    [Table("test_case_2")]
    public class TestClassCase2 : IDbObject
    {
        [ExplicitKey]
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public DateTime Date { get; set; }

        [Column("meta_tags")]
        public string? Meta { get; set; }
    }
}