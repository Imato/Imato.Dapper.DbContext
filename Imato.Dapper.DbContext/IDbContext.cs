using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Imato.Dapper.DbContext
{
    public interface IDbContext
    {
        void AddCommand(ContextCommand command);

        Task BulkInsertAsync<T>(IEnumerable<T> values, string? tableName = null, IEnumerable<string>? columns = null, int bulkCopyTimeoutSeconds = 30, int batchSize = 10000) where T : class;

        ContextCommand Command(string name);

        ContextCommand? Command(string name, IDbConnection connection);

        ContextCommand? CommandRequred(string name);

        ContextCommand CommandRequred(string name, IDbConnection connection);

        string DbName();

        Task DeleteAsync<T>(T value) where T : class;

        void Dispose();

        Task ExecuteAsync(string command, object[]? formatParameters = null);

        Task<IEnumerable<T>> GetAllAsync<T>() where T : class;

        Task InsertAsync<T>(T value) where T : class;

        bool IsDbActive();

        bool IsMasterServer();

        Task<IEnumerable<dynamic>> QueryAsync(string query, object parameters);

        Task<IEnumerable<T>> QueryAsync<T>(string command, object? parameters = null);

        Task<IEnumerable<T>> QueryAsync<T>(string commandName, object[]? formatParameters = null);

        Task<T> QueryFirstAsync<T>(string command, object? parameters = null);

        Task TruncateAsync(string table);

        Task TruncateAsync<T>() where T : class;

        Task UpdateAsync<T>(T value) where T : class;

        Task UpsertAsync<T>(IEnumerable<T> values) where T : class, IDbObjectIdentity;

        string DbObjectTable<T>();

        string DbObjectDb<T>();
    }
}