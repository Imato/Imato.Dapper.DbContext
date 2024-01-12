using System.ComponentModel.DataAnnotations.Schema;

namespace Imato.Dapper.DbContext.Test
{
    [Connection("mssql")]
    [Table("test_case_3")]
    public class TestClassCase3 : IDbObject
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public DateTime Date { get; set; }

        public string? Meta { get; set; }
    }
}