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

        Task BulkInsertAsync<T>(IEnumerable<T> values, string? tableName = null, IEnumerable<string>? columns = null, int bulkCopyTimeoutSeconds = 30, int batchSize = 10000, bool skipFieldsCheck = false) where T : class;

        ContextCommand Command(string name);

        ContextCommand? Command(string name, IDbConnection connection);

        ContextCommand? CommandRequred(string name);

        ContextCommand CommandRequred(string name, IDbConnection connection);

        /// <summary>
        /// Create new connection
        /// </summary>
        /// <param name="connectionString">Connection string or name from appsettings.json</param>
        /// <returns></returns>
        IDbConnection Connection(string connectionString = "");

        string DbName(string connectionName = "");

        Task DeleteAsync<T>(T value) where T : class;

        void Dispose();

        /// <summary>
        ///
        /// </summary>
        /// <param name="command">Command name from config or SQL</param>
        /// <param name="formatParameters"></param>
        /// <param name="connectionStringName"></param>
        /// <returns></returns>
        Task ExecuteAsync(string command, object[]? formatParameters = null, string connectionStringName = "");

        /// <summary>
        ///
        /// </summary>
        /// <param name="command">Command name from config or SQL</param>
        /// <param name="parameters"></param>
        /// <param name="connectionStringName"></param>
        /// <returns></returns>
        Task ExecuteAsync(string command, object? parameters = null, string connectionStringName = "");

        Task<IEnumerable<T>> GetAllAsync<T>() where T : class;

        Task<T> GetAsync<T>(object key) where T : class;

        Task InsertAsync<T>(T value) where T : class;

        Task InsertAsync<T>(IEnumerable<T> values) where T : class;

        bool IsDbActive(string connectionName = "");

        bool IsMasterServer(string connectionName = "");

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

        Task TruncateAsync(string table, string connectionName = "");

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
        /// Connection name of class T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        string ConnectionNameOf<T>();

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