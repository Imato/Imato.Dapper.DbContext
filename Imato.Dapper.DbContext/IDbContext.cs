using System.Collections.Generic;
using System.Threading.Tasks;

namespace Imato.Dapper.DbContext
{
    public interface IDbContext
    {
        string DbName();

        Task DeleteAsync<T>(T value) where T : class;

        void Dispose();

        Task<IEnumerable<T>> GetAllAsync<T>() where T : class;

        Task InsertAsync<T>(T value) where T : class;

        Task UpsertAsync<T>(IEnumerable<T> values) where T : class, IDbObjectIdentity;

        bool IsDbActive();

        bool IsMasterServer();

        Task<IEnumerable<dynamic>> QueryAsync(string query, object parameters);

        Task UpdateAsync<T>(T value) where T : class;
    }
}