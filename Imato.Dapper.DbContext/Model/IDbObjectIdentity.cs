namespace Imato.Dapper.DbContext
{
    public interface IDbObjectIdentity : IDbObject
    {
        public int Id { get; set; }
    }
}