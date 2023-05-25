using Dapper;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Imato.Dapper.DbContext.Example
{
    internal class AppService
    {
        private readonly ILogger<AppService> _logger;
        private readonly ContextFactory _contextFactory;

        public AppService(ContextFactory contextFactory,
            ILogger<AppService> logger)
        {
            _logger = logger;
            _contextFactory = contextFactory;
        }

        public async Task RunAsync()
        {
            await TestMsSql();
            await TestPostgres();
            await TestMySql();
        }

        private void Print(IEnumerable<dynamic>? data)
        {
            _logger.LogInformation($"Result:");
            _logger.LogInformation(JsonSerializer.Serialize(data));
        }

        private async Task TestMsSql()
        {
            _logger.LogInformation("Test mssql");
            var sql = @"
select session_id, connect_time from sys.dm_exec_connections where auth_scheme = @name";
            using var context = _contextFactory.GetDbContext(ContextProviders.mssql);
            using var connection = context.GetConnection();
            var resutl = await connection.QueryAsync<dynamic>(sql, new { name = "SQL" });
            Print(resutl);
        }

        private async Task TestPostgres()
        {
            _logger.LogInformation("Test postgres");
            var sql = @"
select a.pid, a.backend_start from pg_stat_activity a where a.usename = @name";
            using var context = _contextFactory.GetDbContext(ContextProviders.postgres);
            using var connection = context.GetConnection();
            var resutl = await connection.QueryAsync<dynamic>(sql, new { name = "postgres" });
            Print(resutl);
        }

        private async Task TestMySql()
        {
            _logger.LogInformation("Test mysql");
            var sql = @"
select id, host from information_schema.processlist where user = @name";
            using var context = _contextFactory.GetDbContext(ContextProviders.mysql);
            using var connection = context.GetConnection();
            var resutl = await connection.QueryAsync<dynamic>(sql, new { name = "root" });
            Print(resutl);
        }
    }
}