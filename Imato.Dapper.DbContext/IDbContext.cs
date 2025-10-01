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

        /// <summary>
        /// Bulk insert data into table
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="tableName">Table name or [Table] attribute in type T</param>
        /// <param name="columns">Table columns list</param>
        /// <param name="skipFieldsCheck">
        /// Don`t find columns from list in type T, use all fields in T
        /// </param>
        Task BulkInsertAsync<T>(IEnumerable<T> values, string? tableName = null, IEnumerable<string>? columns = null, int bulkCopyTimeoutSeconds = 30, int batchSize = 10000, bool skipFieldsCheck = false) where T : class;

        /// <summary>
        /// Bulk insert data into table
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="connectionStringName"></param>
        /// <param name="tableName">Table name or [Table] attribute in type T</param>
        /// <param name="columns">Table columns list</param>
        /// <param name="skipFieldsCheck">
        /// Don`t find columns from list in type T, use all fields in T
        /// </param>
        Task BulkInsertAsync<T>(IEnumerable<T> data,
            string connectionStringName,
            string tableName,
            IEnumerable<string>? columns = null,
            int bulkCopyTimeoutSeconds = 30,
            int batchSize = 10000,
            bool skipFieldsCheck = false)
            where T : class;

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
        /// </summary>
        /// <param name="command">Command name from config or SQL</param>
        /// <param name="formatParameters"></param>
        /// <param name="connectionStringName"></param>
        /// <returns></returns>
        Task ExecuteAsync(string command, object[]? formatParameters = null, string connectionStringName = "");

        /// <summary>
        /// </summary>
        /// <param name="command">Command name from config or SQL</param>
        /// <param name="parameters"></param>
        /// <param name="connectionStringName"></param>
        /// <returns></returns>
        Task ExecuteAsync(string command, object parameters, string connectionStringName = "");

        Task<IEnumerable<T>> GetAllAsync<T>() where T : class;

        Task<T> GetAsync<T>(object key) where T : class;

        Task<IEnumerable<T>> SelectAsync<T>(string where = null, dynamic? parameters = null) where T : class;

        Task InsertAsync<T>(T value) where T : class;

        Task UpsertAsync<T>(T value) where T : class;

        Task InsertAsync<T>(IEnumerable<T> values) where T : class;

        bool IsDbActive(string connectionName = "");

        bool IsReadOnly(string connectionName = "");

        bool IsMasterServer(string connectionName = "");

        /// <summary>
        /// </summary>
        /// <param name="command">Command name from config or SQL</param>
        Task<IEnumerable<dynamic>> QueryAsync(string command, object parameters);

        /// <summary>
        /// </summary>
        /// <param name="command">Command name from config or SQL</param>
        Task<IEnumerable<T>> QueryAsync<T>(string command, object parameters);

        /// <summary>
        /// </summary>
        /// <param name="command">Command name from config or SQL</param>

        Task<IEnumerable<T>> QueryAsync<T>(string commandName, object[]? formatParameters = null);

        /// <summary>
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
        /// DB of class T from attribute
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
        Task<IEnumerable<TableColumn>> GetColumnsAsync(
            string tableName,
            string connectionName = "");

        /// <summary>
        /// Get columns of tableName
        /// </summary>
        Task<IEnumerable<TableColumn>> GetColumnsAsync<T>(string connectionName = "");

        /// <summary>
        /// Find table in all connections by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        Task<(IDbConnection? connection, string? tableName)> FindTableAsync(string name);

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

        /// <summary>
        /// Check connection in config file
        /// </summary>
        /// <param name="connectionStringName"></param>
        /// <returns></returns>
        bool ConnectionExist(string connectionStringName);

        /// <summary>
        /// Get current context or connection vendor
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public ContextVendors Vendor(IDbConnection? connection = null);
    }
}