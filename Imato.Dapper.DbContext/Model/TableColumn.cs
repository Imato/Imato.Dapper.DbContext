namespace Imato.Dapper.DbContext
{
    public class TableColumn
    {
        public string Name { get; set; } = null!;
        public bool IsComputed { get; set; }
        public bool IsIdentity { get; set; }
    }
}