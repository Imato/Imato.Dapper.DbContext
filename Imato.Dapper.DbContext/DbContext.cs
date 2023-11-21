using Dapper;
using Dapper.Contrib.Extensions;
using Imato.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Npgsql;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Imato.Dapper.DbContext
{
    public class DbContext : IDisposable, IDbContext
    {
        protected readonly ILogger Logger;
        protected readonly IConfiguration Configuration;

        private string _dbName = "";
        private readonly ConcurrentDictionary<string, IDbConnection> _pool = new ConcurrentDictionary<string, IDbConnection>();
        protected readonly List<ContextCommand> ContextCommands = new List<ContextCommand>();

        public DbContext(
            IConfiguration configuration,
            ILogger<DbContext> logger)
        {
            Configuration = configuration;
            Logger = logger;
            AddCommands();
        }

        protected virtual void AddCommands()
        {
            LoadCommandsFrom("*.Sql.Commands.json");
        }

        protected void LoadCommandsFrom(string fileName)
        {
            foreach (var file in Directory.GetFiles(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    fileName,
                    SearchOption.AllDirectories))
            {
                Logger.LogDebug($"Load sql commands from confgiguration file {file}");
                var commands = File.ReadAllText(file)
                    .Deserialize<IEnumerable<ContextCommand>>();

                if (commands != null)
                {
                    foreach (var command in commands)
                    {
                        AddCommand(command);
                    }
                }
            }
        }

        public string DbName()
        {
            var cs = ConnectionString();

            if (_dbName == "")
            {
                switch (Vendor(cs))
                {
                    case ContextVendors.mssql:
                        var m = new SqlConnectionStringBuilder(cs);
                        _dbName = m.InitialCatalog;
                        break;

                    case ContextVendors.postgres:
                        var p = new NpgsqlConnectionStringBuilder(cs);
                        _dbName = p.Database ?? "";
                        break;

                    case ContextVendors.mysql:
                        var s = new MySqlConnectionStringBuilder(cs);
                        _dbName = s.Database ?? "";
                        break;
                }
            }

            return _dbName;
        }

        protected DbConnectionStringBuilder Builder(string connectionString)
        {
            switch (Vendor(connectionString))
            {
                case ContextVendors.mssql:
                    return new SqlConnectionStringBuilder(connectionString);

                case ContextVendors.postgres:
                    return new NpgsqlConnectionStringBuilder(connectionString);

                case ContextVendors.mysql:
                    return new MySqlConnectionStringBuilder(connectionString);
            }

            throw new ArgumentOutOfRangeException($"Unknown vendor in connection string {connectionString}");
        }

        protected string ConnectionString(string name = "")
        {
            if (name == "")
            {
                return Configuration.GetSection("ConnectionStrings")
                    .GetChildren()
                    .FirstOrDefault()
                    ?.Value
                    ?? throw new ArgumentException("Not exists ConnectionStrings in app configuration");
            }

            return Configuration.GetConnectionString(name)
                ?? throw new KeyNotFoundException($"Not exists connection string {name}");
        }

        protected static ContextVendors Vendor(IDbConnection connection)
        {
            return Vendor(connection.ConnectionString);
        }

        protected static ContextVendors Vendor(string connectionString)
        {
            if (connectionString.Contains("Initial Catalog"))
            {
                return ContextVendors.mssql;
            }

            if (connectionString.Contains("Host")
                && connectionString.Contains("Database"))
            {
                return ContextVendors.postgres;
            }

            if (connectionString.Contains("Server")
                && connectionString.Contains("Database"))
            {
                return ContextVendors.mysql;
            }

            throw new InvalidOperationException($"Unknown context vendor for string: {connectionString}");
        }

        protected string DbUser()
        {
            var name = Configuration
                .GetSection("Environment:DbUser")
                .Value;
            return string.IsNullOrEmpty(name) ? ""
                    : Environment.GetEnvironmentVariable(name);
        }

        protected string DbUserPassword()
        {
            var name = Configuration
                .GetSection("Environment:DbUserPassword")
                .Value;
            return string.IsNullOrEmpty(name) ? ""
                    : Environment.GetEnvironmentVariable(name);
        }

        public static IDbConnection Create(string connectionString,
            string dataBase = "",
            string user = "",
            string password = "")
        {
            switch (Vendor(connectionString))
            {
                case ContextVendors.mssql:
                    var sb = new SqlConnectionStringBuilder(connectionString);
                    sb.InitialCatalog = dataBase != "" ? dataBase : sb.InitialCatalog;
                    sb.UserID = sb.UserID ?? user;
                    sb.Password = sb.Password ?? password;
                    return new SqlConnection(sb.ConnectionString);

                case ContextVendors.postgres:
                    var nb = new NpgsqlConnectionStringBuilder(connectionString);
                    nb.Database = dataBase != "" ? dataBase : nb.Database;
                    nb.Username = nb.Username ?? user;
                    nb.Password = nb.Password ?? password;
                    return new NpgsqlConnection(nb.ConnectionString);

                case ContextVendors.mysql:
                    var mb = new MySqlConnectionStringBuilder(connectionString);
                    mb.Database = dataBase != "" ? dataBase : mb.Database;
                    mb.UserID = mb.UserID ?? user;
                    mb.Password = mb.Password ?? password;
                    return new MySqlConnection(mb.ConnectionString);
            }

            throw new ArgumentOutOfRangeException($"Unknown vendor in connection string {connectionString}");
        }

        protected IDbConnection Create(string connectionString,
            string dataBase = "")
        {
            var user = DbUser();
            var password = DbUserPassword();
            var cs = Create(connectionString, dataBase, user, password);
            Logger.LogDebug($"Create connection: {cs.ConnectionString}");
            return cs;
        }

        protected IDbConnection Connection(
            string dataBase = "",
            string connectionName = "",
            string connectionStringName = "")
        {
            if (!string.IsNullOrEmpty(connectionName)
                && _pool.ContainsKey(connectionName)
                && IsReady(_pool[connectionName]))
            {
                return _pool[connectionName];
            }

            var connectionString = ConnectionString(connectionStringName);
            var connection = Create(connectionString, dataBase);

            if (!string.IsNullOrEmpty(connectionName))
            {
                _pool.AddOrUpdate(connectionName,
                    (_) => connection,
                    (_, old) => IsReady(old) ? old : connection);
            }

            return connection;
        }

        protected IDbConnection Connection(ContextVendors vendor)
        {
            var cs = Configuration.GetSection("ConnectionStrings")
                    .GetChildren()
                    .Select(x => x.Value)
                    .Where(x => Vendor(x ?? "") == vendor)
                    .FirstOrDefault();

            if (cs == null)
            {
                throw new ArgumentOutOfRangeException($"Cannot find connection string for vendor {vendor}");
            }

            return Create(cs, "", DbUser(), DbUserPassword());
        }

        public ContextCommand Command(string name)
        {
            var vendor = Vendor(ConnectionString());
            return ContextCommands
                .Where(x => x.Name == name && x.ContextVendor == vendor)
                .FirstOrDefault();
        }

        public ContextCommand? CommandRequred(string name)
        {
            return Command(name)
                ?? throw new ArgumentOutOfRangeException($"Cannot find context command {name}. Add it in AddCommands()");
        }

        public ContextCommand? Command(string name, IDbConnection connection)
        {
            var vendor = Vendor(connection);
            return ContextCommands
                .Where(x => x.Name == name && x.ContextVendor == vendor)
                .FirstOrDefault();
        }

        public ContextCommand CommandRequred(string name, IDbConnection connection)
        {
            return Command(name, connection)
                ?? throw new ArgumentOutOfRangeException($"Cannot find context command {name}. Add it in AddCommands()");
        }

        public void AddCommand(ContextCommand command)
        {
            Logger.LogDebug($"Add command {command.ToJson()}");
            var vendor = Vendor(ConnectionString());
            var exits = ContextCommands.Find(x => x.ContextVendor == command.ContextVendor && x.Name == command.Name);
            if (exits != null)
            {
                ContextCommands.Remove(exits);
            }
            ContextCommands.Add(command);
        }

        public bool IsMasterServer()
        {
            using (var connection = Connection())
            {
                var command = CommandRequred("IsMasterServer", connection);
                var sql = Format(command.Text);
                return connection.QueryFirst<string>(sql)
                    .StartsWith(Environment.MachineName);
            }
        }

        private string Format(string sql, object[]? parameters = null)
        {
            if (parameters != null
                && sql.Contains("{")
                && sql.Contains("}"))
            {
                sql = string.Format(sql, parameters);
            }

            Logger.LogDebug($"Using sql: {sql}");
            return sql;
        }

        private string Format(string sql, object? parameters)
        {
            Logger.LogDebug($"Using sql: {sql}; parameters: {parameters?.ToJson() ?? null}");
            return sql;
        }

        private string Format(string sql)
        {
            Logger.LogDebug($"Using sql: {sql}");
            return sql;
        }

        /// <summary>
        /// Format sql text and execute
        /// </summary>
        /// <param name="command">Command from config</param>
        /// <param name="formatParameters">Replace {} in sql</param>
        /// <returns></returns>
        public async Task ExecuteAsync(string command,
            object[]? formatParameters = null)
        {
            using (var connection = Connection())
            {
                var sql = Command(command, connection)?.Text ?? command;
                await connection.ExecuteAsync(Format(sql, formatParameters));
            }
        }

        /// <summary>
        /// Format sql text and query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="commandName">Command from config</param>
        /// <param name="formatParameters">Replace {} in sql</param>
        /// <returns></returns>
        public async Task<IEnumerable<T>> QueryAsync<T>(string commandName,
            object[]? formatParameters = null)
        {
            using (var connection = Connection())
            {
                var command = CommandRequred(commandName, connection);
                return await connection.QueryAsync<T>(Format(command.Text, formatParameters));
            }
        }

        public bool IsDbActive()
        {
            using (var connection = Connection())
                return QueryFirstAsync<bool>("IsDbActive",
                        new { name = DbName() })
                        .Result;
        }

        protected bool IsReady(IDbConnection? connection)
        {
            return connection != null
                && connection.State != ConnectionState.Closed
                && connection.State != ConnectionState.Broken;
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string command,
            object? parameters = null)
        {
            using (var connection = Connection())
            {
                var sql = Command(command, connection)?.Text ?? command;
                return await connection.QueryAsync<T>(
                    Format(sql, parameters),
                    parameters);
            }
        }

        public async Task<IEnumerable<dynamic>> QueryAsync(string query,
            object parameters)
        {
            using var c = Connection();
            return await c.QueryAsync<dynamic>(Format(query, parameters), parameters)
                ?? Array.Empty<dynamic>();
        }

        public async Task<T> QueryFirstAsync<T>(string command,
            object? parameters = null)
        {
            using (var connection = Connection())
            {
                var sql = Command(command, connection)?.Text ?? command;
                return await connection.QueryFirstAsync<T>(
                    Format(sql, parameters),
                    parameters);
            }
        }

        public async Task<IEnumerable<T>> GetAllAsync<T>() where T : class
        {
            using var c = Connection();
            return await c.GetAllAsync<T>();
        }

        public async Task InsertAsync<T>(T value) where T : class
        {
            using var c = Connection();
            await c.InsertAsync(value);
        }

        public async Task UpdateAsync<T>(T value) where T : class
        {
            using var c = Connection();
            await c.UpdateAsync(value);
        }

        public async Task TruncateAsync(string table)
        {
            using var c = Connection();
            var sql = "truncate";
            switch (Vendor(c))
            {
                case ContextVendors.mssql:
                    sql += " table " + MsSql.FromatTableName(table);
                    break;

                case ContextVendors.postgres:
                    sql += " " + Postgres.FromatTableName(table) + " restart identity";
                    break;

                case ContextVendors.mysql:
                    sql += " table " + table;
                    break;
            }
            await c.ExecuteAsync(Format(sql));
        }

        public async Task TruncateAsync<T>() where T : class
        {
            var table = Model.GetTable<T>();
            using var c = Connection();
            await TruncateAsync(table);
        }

        /// <summary>
        /// Use for small tables only!
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task UpsertAsync<T>(IEnumerable<T> values)
            where T : class, IDbObjectIdentity
        {
            if (values == null)
            {
                return;
            }

            using var c = Connection();
            var all = await c.GetAllAsync<T>();
            var exists = values.Where(x => all.Any(y => y.Id == x.Id));
            if (exists.Any())
            {
                await c.UpdateAsync(exists);
            }
            var news = values.Where(x => !all.Any(y => y.Id == x.Id));
            if (news.Any())
            {
                await c.InsertAsync(news);
            }
            var delete = all.Where(x => !values.Any(y => y.Id == x.Id));
            if (delete.Any())
            {
                await c.DeleteAsync(delete);
            }
        }

        public async Task DeleteAsync<T>(T value) where T : class
        {
            using var c = Connection();
            await c.DeleteAsync(value);
        }

        public async Task BulkInsertAsync<T>(IEnumerable<T> values,
            string? tableName = null,
            IEnumerable<string>? columns = null,
            int bulkCopyTimeoutSeconds = 30,
            int batchSize = 10000)
            where T : class
        {
            using (var c = Connection())
            {
                var vendor = Vendor(c);
                switch (vendor)
                {
                    case ContextVendors.mssql:
                        var mc = (c as SqlConnection) ?? throw new ApplicationException("Wrong connection. Not MSSQL.");
                        await MsSqlBulkCopy.BulkInsertAsync(mc, values, tableName, columns, bulkCopyTimeoutSeconds, batchSize);
                        break;

                    case ContextVendors.postgres:
                        var nc = (c as NpgsqlConnection) ?? throw new ApplicationException("Wrong connection. Not Postgres.");
                        await PostgresBulkCopy.BulkInsertAsync(nc, values, tableName, columns);
                        break;

                    default:
                        throw new NotImplementedException($"Cannot use bulk insert for {vendor}");
                }
            }
        }

        public void Dispose()
        {
            foreach (var p in _pool)
            {
                if (p.Value.State != ConnectionState.Closed)
                {
                    Logger?.LogDebug($"Close connection {p.Key}");
                    p.Value.Close();
                }
            }
        }
    }
}