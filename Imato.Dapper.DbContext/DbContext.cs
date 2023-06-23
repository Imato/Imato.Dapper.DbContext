using Dapper;
using Dapper.Contrib.Extensions;
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
using System.Linq;
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
            ContextCommands.Add(new ContextCommand
            {
                Name = "IsMasterServer",
                Text = "select top 1 @@SERVERNAME from sys.tables"
            });
            ContextCommands.Add(new ContextCommand
            {
                Name = "IsMasterServer",
                ContextVendor = ContextVendors.postgres,
                Text = @"
create temp table tt_cmd (hostname text);
copy tt_cmd from program 'hostname';
select * from tt_cmd;
drop table tt_cmd;"
            });

            ContextCommands.Add(new ContextCommand
            {
                Name = "IsDbActive",
                Text = @"
declare @status bit = 0;
select @status = cast(1 as bit)
	from sys.databases
	where name = @name
		and user_access_desc = 'MULTI_USER'
		and state_desc = 'ONLINE'
select @status"
            });
            ContextCommands.Add(new ContextCommand
            {
                Name = "IsDbActive",
                ContextVendor = ContextVendors.postgres,
                Text = @"
select result
from
(select true as result from pg_database d where d.datname = @name
union all select false) t
order by 1 desc
limit 1"
            });
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

        protected ContextVendors Vendor(IDbConnection connection)
        {
            return Vendor(connection.ConnectionString);
        }

        protected ContextVendors Vendor(string connectionString)
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
                && connectionString.Contains("Database")
                && connectionString.Contains("Uid"))
            {
                return ContextVendors.mysql;
            }

            throw new InvalidOperationException($"Unknown context vendor for string: {connectionString}");
        }

        protected IDbConnection Create(string connectionString,
            string dataBase = "")
        {
            switch (Vendor(connectionString))
            {
                case ContextVendors.mssql:
                    if (dataBase != "")
                    {
                        var builder = new SqlConnectionStringBuilder(connectionString);
                        builder.InitialCatalog = dataBase;
                        connectionString = builder.ConnectionString;
                    }
                    return new SqlConnection(connectionString);

                case ContextVendors.postgres:
                    if (dataBase != "")
                    {
                        var builder = new NpgsqlConnectionStringBuilder(connectionString);
                        builder.Database = dataBase;
                        connectionString = builder.ConnectionString;
                    }
                    return new NpgsqlConnection(connectionString);

                case ContextVendors.mysql:
                    if (dataBase != "")
                    {
                        var builder = new MySqlConnectionStringBuilder(connectionString);
                        builder.Database = dataBase;
                        connectionString = builder.ConnectionString;
                    }
                    return new MySqlConnection(connectionString);
            }

            throw new ArgumentOutOfRangeException($"Unknown vendor in connection string {connectionString}");
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

            return Create(cs);
        }

        protected ContextCommand Command(string name)
        {
            var vendor = Vendor(ConnectionString());
            return ContextCommands
                .Where(x => x.Name == name && x.ContextVendor == vendor)
                .FirstOrDefault()
                ?? throw new ArgumentOutOfRangeException($"Cannot find context command {name}. Add it in AddCommands()");
        }

        protected ContextCommand Command(string name, IDbConnection connection)
        {
            var vendor = Vendor(connection);
            return ContextCommands
                .Where(x => x.Name == name && x.ContextVendor == vendor)
                .FirstOrDefault()
                ?? throw new ArgumentOutOfRangeException($"Cannot find context command {name}. Add it in AddCommands()");
        }

        public bool IsMasterServer()
        {
            using (var connection = Connection())
            {
                var command = Command("IsMasterServer", connection);
                return connection.QueryFirst<string>(command.Text)
                    .StartsWith(Environment.MachineName);
            }
        }

        public bool IsDbActive()
        {
            using (var connection = Connection())
                return connection.QueryFirst<bool>(
                        Command("IsDbActive", connection).Text,
                        new { name = DbName() });
        }

        protected bool IsReady(IDbConnection? connection)
        {
            return connection != null
                && connection.State != ConnectionState.Closed
                && connection.State != ConnectionState.Broken;
        }

        public async Task<IEnumerable<dynamic>> QueryAsync(string query,
            object parameters)
        {
            using var c = Connection();
            return await c.QueryAsync<dynamic>(query, parameters)
                ?? Array.Empty<dynamic>();
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

        public void Dispose()
        {
            foreach (var p in _pool)
            {
                if (p.Value.State != ConnectionState.Closed)
                {
                    Logger.LogDebug($"Close connection {p.Key}");
                    p.Value.Close();
                }
            }
        }
    }
}