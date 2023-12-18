namespace Imato.Dapper.DbContext
{
    public class ConnectionString
    {
        public string Name { get; set; } = null!;
        public string String { get; set; } = null!;
        public ContextVendors Vendor { get; set; }
    }
}