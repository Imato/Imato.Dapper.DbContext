using Dapper;
using Imato.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Npgsql;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Imato.Dapper.DbContext
{
    public class DbContext : IDisposable, IDbContext
    {
        private readonly string? _connectionString;
        private string _dbName = "";
        private static ConcurrentDictionary<string, IDbConnection> _pool = new ConcurrentDictionary<string, IDbConnection>();
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private static bool _initilized = false;

        protected readonly ILogger? Logger;
        protected readonly IConfiguration Configuration;
        protected static List<ContextCommand> ContextCommands = new List<ContextCommand>();

        public DbContext(
            IConfiguration configuration,
            ILogger<DbContext>? logger = null)
        {
            Configuration = configuration;
            Logger = logger;
            Initilize();
        }

        public DbContext(
            string connectionString,
            ILogger<DbContext>? logger = null)
        {
            _connectionString = connectionString;
            Logger = logger;
            Initilize();
        }

        private void Initilize()
        {
            _semaphore.Wait();
            if (!_initilized)
            {
                try
                {
                    RegisterTypes(typeof(DbContext).Assembly);
                    LoadCommands();
                    RunMigrations().Wait();
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex, "DbContext initialization");
                }

                _initilized = true;
            }

            _semaphore.Release();
        }

        protected void LoadCommands()
        {
            var folder = Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location);
            folder = Path.Combine(folder, "SqlCommands");

            foreach (var file in new DirectoryInfo(folder)
                        .GetFiles("*.sql", SearchOption.AllDirectories)
                        .OrderBy(x => x.LastWriteTime)
                        .Select(x => x.FullName))
            {
                try
                {
                    var sql = File.ReadAllText(file);
                    var name = Path.GetFileNameWithoutExtension(file);
                    var vendor = Path.GetDirectoryName(file)
                        .Split(Path.DirectorySeparatorChar)
                        .Last();
                    var command = new ContextCommand
                    {
                        Name = name,
                        ContextVendor = (ContextVendors)Enum.Parse(
                            typeof(ContextVendors), vendor),
                        Text = sql
                    };
                    Add(command);
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex, $"LoadCommand from {file}");
                    // Console.Error.WriteLine($"LoadCommand from {file} error: {ex}");
                }
            }
        }

        protected async Task RunMigrations()
        {
            var folder = Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location);
            folder = Path.Combine(folder, "Migrations");

            var connections = ConnectionStrings()
                .ToDictionary(x => x.Vendor.ToString(), x => Create(x.String));

            if (connections.Count == 0)
            {
                return;
            }

            foreach (var c in connections.Values)
            {
                await c.ExecuteAsync(Sql("CreateMigrations", c));
            }

            if (Directory.Exists(folder)
                && connections.Count > 0)
            {
                foreach (var file in new DirectoryInfo(folder)
                            .GetFiles("*.sql", SearchOption.AllDirectories)
                            .OrderBy(x => x.Name)
                            .Select(x => x.FullName))
                {
                    var name = Path.GetFileName(file);
                    var vendor = Path.GetDirectoryName(file)
                        .Split(Path.DirectorySeparatorChar)
                        .Last();
                    var connection = connections.ContainsKey(vendor)
                        ? connections[vendor]
                        : null;
                    if (connection == null
                        && connections.Count == 1
                        && !Enum.TryParse(typeof(ContextVendors), vendor, out var r))
                    {
                        connection = connections.Values.First();
                    }

                    if (connection != null)
                    {
                        try
                        {
                            var exists = await connection.GetAsync<DbMigration>(name);
                            if (exists == null)
                            {
                                var sql = File.ReadAllText(file);

                                if (Vendor(connection) == ContextVendors.mssql)
                                {
                                    var re = new Regex("^go\r",
                                    RegexOptions.Compiled
                                    | RegexOptions.IgnoreCase
                                    | RegexOptions.Multiline);
                                    if (!string.IsNullOrEmpty(sql))
                                    {
                                        foreach (var s in re.Split(sql))
                                        {
                                            await connection.ExecuteAsync(Sql(s));
                                        }
                                    }
                                }
                                else
                                {
                                    await connection.ExecuteAsync(Sql(sql));
                                }
                            }

                            await connection.InsertAsync(new DbMigration
                            {
                                Id = name,
                                Date = DateTime.Now
                            });
                        }
                        catch (Exception ex)
                        {
                            Logger?.LogError(ex, $"Error in migration file {file}");
                        }
                    }
                }
            }
        }

        public string DbName()
        {
            var cs = RequiredConnectionString();

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

        protected string? ConnectionString(string name = "")
        {
            return _connectionString ??
                Configuration
                ?.GetSection("ConnectionStrings")
                ?.GetChildren()
                ?.FirstOrDefault(x => string.IsNullOrEmpty(name) || x.Key == name)
                ?.Value;
        }

        protected string RequiredConnectionString(string name = "")
        {
            return ConnectionString()
                ?? throw new ArgumentException($"Not exists ConnectionStrings {name} in app configuration");
        }

        protected IEnumerable<ConnectionString> ConnectionStrings()
        {
            return Configuration
                    ?.GetSection("ConnectionStrings")
                    ?.GetChildren()
                    ?.Select(c => new ConnectionString
                    {
                        Name = c.Key,
                        String = c.Value ?? throw new ApplicationException($"Empty connection string {c.Key}"),
                        Vendor = Vendor(c.Value)
                    })
                    ??
                    (_connectionString != null
                        ? new ConnectionString[]
                        {
                            new ConnectionString
                            {
                                Name = "CS",
                                String = _connectionString,
                                Vendor = Vendor(_connectionString)
                            }
                        }
                        : Array.Empty<ConnectionString>());
        }

        protected ContextVendors Vendor(IDbConnection? connection)
        {
            return Vendor(connection?.ConnectionString ?? RequiredConnectionString());
        }

        protected static ContextVendors Vendor(string connectionString)
        {
            var cs = connectionString;

            if (cs.Contains("Initial Catalog"))
            {
                return ContextVendors.mssql;
            }

            if (cs.Contains("Host")
                && cs.Contains("Database"))
            {
                return ContextVendors.postgres;
            }

            if (cs.Contains("Server")
                && cs.Contains("Database"))
            {
                return ContextVendors.mysql;
            }

            throw new InvalidOperationException($"Unknown context vendor for string: {connectionString}");
        }

        protected string DbUser()
        {
            var name = Configuration
                ?.GetSection("Environment:DbUser")
                ?.Value;
            return string.IsNullOrEmpty(name) ? ""
                    : Environment.GetEnvironmentVariable(name) ?? "";
        }

        protected string DbUserPassword()
        {
            var name = Configuration
                ?.GetSection("Environment:DbUserPassword")
                ?.Value;
            return string.IsNullOrEmpty(name) ? ""
                    : Environment.GetEnvironmentVariable(name) ?? "";
        }

        public IDbConnection Create(string connectionString,
            string dataBase = "",
            string user = "",
            string password = "")
        {
            var cs = ConnectionString(connectionString) ?? connectionString;

            switch (Vendor(cs))
            {
                case ContextVendors.mssql:
                    var sb = new SqlConnectionStringBuilder(cs);
                    sb.InitialCatalog = dataBase != "" ? dataBase : sb.InitialCatalog;
                    sb.UserID = string.IsNullOrEmpty(sb.UserID) ? user : sb.UserID;
                    sb.Password = string.IsNullOrEmpty(sb.Password) ? password : sb.Password;
                    return new SqlConnection(sb.ConnectionString);

                case ContextVendors.postgres:
                    var nb = new NpgsqlConnectionStringBuilder(cs);
                    nb.Database = dataBase != "" ? dataBase : nb.Database;
                    nb.Username = string.IsNullOrEmpty(nb.Username) ? user : nb.Username;
                    nb.Password = string.IsNullOrEmpty(nb.Password) ? password : nb.Password;
                    return new NpgsqlConnection(nb.ConnectionString);

                case ContextVendors.mysql:
                    var mb = new MySqlConnectionStringBuilder(cs);
                    mb.Database = dataBase != "" ? dataBase : mb.Database;
                    mb.UserID = string.IsNullOrEmpty(mb.UserID) ? user : mb.UserID;
                    mb.Password = string.IsNullOrEmpty(mb.Password) ? password : mb.Password;
                    return new MySqlConnection(mb.ConnectionString);
            }

            throw new ArgumentOutOfRangeException($"Unknown vendor in connection string {cs}");
        }

        public IDbConnection Create(string connectionString,
            string dataBase = "")
        {
            var user = DbUser();
            var password = DbUserPassword();
            var cs = Create(connectionString, dataBase, user, password);
            return cs;
        }

        /// <summary>
        /// Connection string or name from appsettings.json
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public IDbConnection Create(string connectionString = "")
        {
            return Create(connectionString, "");
        }

        protected IDbConnection Connection<T>()
        {
            return Connection(dbName: DbAttribute.Value<T>(),
                connectionStringName: ConnectionAttribute.Value<T>());
        }

        protected IDbConnection Connection(
            string dbName = "",
            string connectionName = "",
            string connectionStringName = "")
        {
            if (!string.IsNullOrEmpty(connectionName)
                && _pool.ContainsKey(connectionName)
                && IsReady(_pool[connectionName]))
            {
                return _pool[connectionName];
            }

            var connectionString = RequiredConnectionString(connectionStringName);
            var connection = Create(connectionString, dbName);

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
            var cs = Configuration
                    ?.GetSection("ConnectionStrings")
                    ?.GetChildren()
                    ?.Select(x => x.Value)
                    ?.Where(x => Vendor(x ?? "") == vendor)
                    ?.FirstOrDefault();

            if (cs == null)
            {
                throw new ArgumentOutOfRangeException($"Cannot find connection string for vendor {vendor}");
            }

            return Create(cs, "", DbUser(), DbUserPassword());
        }

        public ContextCommand Command(string name)
        {
            var vendor = Vendor(RequiredConnectionString());
            return ContextCommands
                .Where(x => x.Name == name && x.ContextVendor == vendor)
                .FirstOrDefault();
        }

        public ContextCommand? CommandRequred(string name)
        {
            return Command(name)
                ?? throw new ArgumentOutOfRangeException($"Cannot find context command {name}. Add it in AddCommands()");
        }

        public ContextCommand? Command(string name, IDbConnection? connection)
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

        private static void Add(ContextCommand command)
        {
            var exits = ContextCommands.Find(x => x.ContextVendor == command.ContextVendor && x.Name == command.Name);
            if (exits != null)
            {
                ContextCommands.Remove(exits);
            }
            ContextCommands.Add(command);
        }

        public void AddCommand(ContextCommand command)
        {
            Add(command);
        }

        public bool IsMasterServer()
        {
            using (var connection = Connection())
            {
                var sql = Sql("IsMasterServer", connection);
                return Try(() => connection.QueryFirst<string>(sql)
                                           .StartsWith(Environment.MachineName),
                    sql);
            }
        }

        protected string Sql(string command,
            object[]? parameters,
            IDbConnection? connection)
        {
            var sql = Command(command, connection)?.Text ?? command;
            if (parameters != null
                && sql.Contains("{")
                && sql.Contains("}"))
            {
                sql = string.Format(sql, parameters);
            }

            Logger?.LogDebug($"Using sql: {sql}");
            return sql;
        }

        protected string Sql(string command,
            object? parameters,
            IDbConnection? connection)
        {
            var sql = Command(command, connection)?.Text ?? command;
            Logger?.LogDebug($"Using sql: {sql}; parameters: {parameters?.ToJson() ?? null}");
            return sql;
        }

        protected string Sql(string command,
            IDbConnection? connection = null)
        {
            var sql = command.Length < 100
                ? (Command(command, connection)?.Text ?? command)
                : command;
            Logger?.LogDebug($"Using sql: {sql}");
            return sql;
        }

        /// <summary>
        /// Format sql text and execute
        /// </summary>
        /// <param name="command">Command name from config or SQL</param>
        /// <param name="formatParameters">Replace {} in sql</param>
        /// <returns></returns>
        public async Task ExecuteAsync(string command,
            object[]? formatParameters = null,
            string connectionStringName = "")
        {
            using (var connection = Connection(connectionStringName: connectionStringName))
            {
                await connection.ExecuteAsync(Sql(command, formatParameters, connection));
            }
        }

        // <summary>
        /// Format sql text and execute
        /// </summary>
        /// <param name="command">Command name from config or SQL</param>
        /// <param name="parameters">sql paramaters</param>
        /// <returns></returns>
        public async Task ExecuteAsync(string command,
            object parameters,
            string connectionStringName = "")
        {
            using (var connection = Connection(connectionStringName: connectionStringName))
            {
                var sql = Sql(command, parameters, connection);
                await Try(() => connection.ExecuteAsync(sql, parameters), sql);
            }
        }

        /// <summary>
        /// Format sql text and query
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command">Command name from config or SQL</param>
        /// <param name="formatParameters">Replace {} in sql</param>
        /// <returns></returns>
        public async Task<IEnumerable<T>> QueryAsync<T>(string command,
            object[]? formatParameters = null)
        {
            using (var connection = Connection<T>())
            {
                var sql = Sql(command, formatParameters, connection);
                return await Try(() => connection.QueryAsync<T>(sql), sql, command);
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

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command">Command name from config or SQL</param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public async Task<IEnumerable<T>> QueryAsync<T>(string command,
            object parameters)
        {
            using (var connection = Connection<T>())
            {
                var sql = Sql(command, parameters, connection);
                return await Try(() => connection.QueryAsync<T>(sql, parameters),
                    sql, command);
            }
        }

        private T Try<T>(Func<T> func,
            string sql,
            string? command = null)
        {
            try
            {
                return func();
            }
            catch (Exception e)
            {
                throw Catch(e, sql, command);
            }
        }

        private Exception Catch(Exception e,
            string sql,
            string? command = null)
        {
            var ex = new SqlException(e, sql, command);
            Logger?.LogError(ex.Message);
            return ex;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="command">Command name from config or SQL</param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public async Task<IEnumerable<dynamic>> QueryAsync(string command,
            object parameters)
        {
            using (var connection = Connection())
            {
                var sql = Sql(command, parameters, connection);
                return await Try(() => connection.QueryAsync<dynamic>(sql, parameters),
                    sql, command);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="command">Command name from config or SQL</param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public async Task<T> QueryFirstAsync<T>(string command,
            object? parameters = null)
        {
            using (var connection = Connection<T>())
            {
                var sql = Sql(command, parameters, connection);
                return await Try(() => connection.QueryFirstAsync<T>(sql, parameters),
                    sql, command);
            }
        }

        public async Task<T> GetAsync<T>(object key) where T : class
        {
            using var c = Connection<T>();
            return await c.GetAsync<T>(key, logger: Logger);
        }

        public async Task<IEnumerable<T>> GetAllAsync<T>() where T : class
        {
            using var c = Connection<T>();
            return await c.GetAllAsync<T>(logger: Logger);
        }

        public async Task InsertAsync<T>(T value) where T : class
        {
            using var c = Connection<T>();
            await c.InsertAsync(value, logger: Logger);
        }

        public async Task InsertAsync<T>(IEnumerable<T> values) where T : class
        {
            using var c = Connection<T>();
            await c.InsertAsync(values, logger: Logger);
        }

        public async Task UpdateAsync<T>(T value) where T : class
        {
            using var c = Connection<T>();
            await c.UpdateAsync(value, logger: Logger);
        }

        private async Task TruncateAsync(IDbConnection connection,
            string table)
        {
            var sql = "truncate";
            switch (Vendor(connection))
            {
                case ContextVendors.mssql:
                    sql += " table " + MsSql.FormatTableName(table);
                    break;

                case ContextVendors.postgres:
                    sql += " " + Postgres.FormatTableName(table) + " restart identity";
                    break;

                case ContextVendors.mysql:
                    sql += " table " + table;
                    break;
            }

            await Try(() => connection.ExecuteAsync(Sql(sql)),
                sql);
        }

        public async Task TruncateAsync<T>() where T : class
        {
            using var c = Connection<T>();
            await TruncateAsync(c, TableNameOf<T>());
        }

        public async Task TruncateAsync(string table)
        {
            using var c = Connection();
            await TruncateAsync(c, table);
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

            using var c = Connection<T>();
            var all = await c.GetAllAsync<T>(logger: Logger);
            var exists = values.Where(x => all.Any(y => y.Id == x.Id));
            if (exists.Any())
            {
                await c.UpdateAsync(exists, logger: Logger);
            }
            var news = values.Where(x => !all.Any(y => y.Id == x.Id));
            if (news.Any())
            {
                await c.InsertAsync(news, logger: Logger);
            }
            var delete = all.Where(x => !values.Any(y => y.Id == x.Id));
            if (delete.Any())
            {
                await c.DeleteAsync(delete, logger: Logger);
            }
        }

        public async Task DeleteAsync<T>(T value) where T : class
        {
            using var c = Connection<T>();
            await c.DeleteAsync(value, logger: Logger);
        }

        public async Task BulkInsertAsync<T>(IEnumerable<T> values,
            string? tableName = null,
            IEnumerable<string>? columns = null,
            int bulkCopyTimeoutSeconds = 30,
            int batchSize = 10000)
            where T : class
        {
            using (var c = Connection<T>())
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

        /// <summary>
        /// Table name of class T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public string TableNameOf<T>()
        {
            return TableAttributeExtensions.RequiredValue<T>();
        }

        /// <summary>
        /// Db name of class T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public string DbNameOf<T>()
        {
            return DbAttribute.RequiredValue<T>();
        }

        /// <summary>
        /// Connection name of class T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public string ConnectionNameOf<T>()
        {
            return ConnectionAttribute.RequiredValue<T>();
        }

        /// <summary>
        /// Get columns of tableName
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public async Task<IEnumerable<string>> GetColumnsAsync(
            string tableName)
        {
            return await QueryAsync<string>("GetColumns", new { tableName });
        }

        /// <summary>
        /// Register DB type with columns mapping
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void RegisterType<T>()
        {
            RegisterType(typeof(T));
        }

        /// <summary>
        /// Register DB type with columns mapping
        /// </summary>
        public void RegisterType(Type type)
        {
            SqlMapperExtensions.RegisterType(type);
        }

        /// <summary>
        /// Register all DB types with columns mapping.
        /// </summary>
        /// <param name="assembly">assembly with DB types</param>
        public void RegisterTypes(Assembly assembly)
        {
            var types = assembly
                .GetTypes()
                .Where(x =>
                    !x.IsAbstract
                    && !x.IsInterface
                    && (x.GetCustomAttribute<TableAttribute>() != null)
                        || x.GetProperties().Any(x => x.GetCustomAttribute<ColumnAttribute>() != null));

            foreach (var type in types)
            {
                RegisterType(type);
            }
        }
    }
}