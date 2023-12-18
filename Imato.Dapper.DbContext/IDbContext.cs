using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
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

        /// <summary>
        ///
        /// </summary>
        /// <param name="command">Command name from config or SQL</param>
        Task<IEnumerable<dynamic>> QueryAsync(string command, object parameters);

        /// <summary>
        ///
        /// </summary>
        /// <param name="command">Command name from config or SQL</param>
        Task<IEnumerable<T>> QueryAsync<T>(string command, object parameters);

        /// <summary>
        ///
        /// </summary>
        /// <param name="command">Command name from config or SQL</param>

        Task<IEnumerable<T>> QueryAsync<T>(string commandName, object[]? formatParameters = null);

        /// <summary>
        ///
        /// </summary>
        /// <param name="command">Command name from config or SQL</param>
        Task<T> QueryFirstAsync<T>(string command, object? parameters = null);

        Task TruncateAsync(string table);

        Task TruncateAsync<T>() where T : class;

        Task UpdateAsync<T>(T value) where T : class;

        /// <summary>
        /// Use for small tables only!
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        Task UpsertAsync<T>(IEnumerable<T> values) where T : class, IDbObjectIdentity;

        /// <summary>
        /// Table in DB of class T from attribute
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        string TableNameOf<T>();

        /// <summary>
        /// DB of class T  from attribute
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        string DbNameOf<T>();

        /// <summary>
        /// Get columns of tableName
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        Task<IEnumerable<string>> GetColumnsAsync(
            string tableName);

        /// <summary>
        /// Register DB type with columns mapping
        /// </summary>
        /// <typeparam name="T"></typeparam>
        void RegisterType<T>();

        /// <summary>
        /// Register DB type with columns mapping
        /// </summary>
        void RegisterType(Type type);

        /// <summary>
        /// Register all DB types with columns mapping.
        /// </summary>
        /// <param name="assembly">assembly with DB types</param>
        void RegisterTypes(Assembly assembly);
    }
}