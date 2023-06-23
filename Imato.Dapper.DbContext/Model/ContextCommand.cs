namespace Imato.Dapper.DbContext
{
    public class ContextCommand
    {
        public string Name { get; set; } = null!;
        public ContextVendors ContextVendor { get; set; } = ContextVendors.mssql;
        public string Text { get; set; } = null!;
    }
}