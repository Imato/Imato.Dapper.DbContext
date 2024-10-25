using Dapper;
using System.Data;
using System.Reflection;

namespace Imato.Dapper.DbContext.Test.Providers
{
    public class PostgresProviderTests
    {
        private DbContext context;

        [OneTimeSetUp]
        public void Setup()
        {
            AppBulder.SetupApp();
            context = AppBulder.GetRequiredService<DbContext>();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            context?.Dispose();
        }

        [Test]
        public async Task BaseConnectionStringTest()
        {
            var str = "Host=localhost;Port=5432;Database=unit_tests;Pooling=true;MinPoolSize=10;MaxPoolSize=100;User ID={UNIT_TESTS_USER};Password={UNIT_TESTS_PASSWORD}";
            var pg = new PostgresProvider(str);
            using var connection = pg.CreateConnection();
            var result = await pg.IsReadWriteConnectionAsync(connection);
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task SecondaryConnectionStringTest()
        {
            var str = "Host=localhost,secondary;Port=5432;Database=unit_tests;Pooling=true;MinPoolSize=10;MaxPoolSize=100;User ID={UNIT_TESTS_USER};Password={UNIT_TESTS_PASSWORD}";
            var pg = new PostgresProvider(str);
            using var connection = pg.CreateConnection();
            var result = await pg.IsReadWriteConnectionAsync(connection);
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task FailbackConnectionStringTest()
        {
            var str = "Host=master,localhost;Port=5432;Database=unit_tests;Pooling=true;MinPoolSize=10;MaxPoolSize=100;User ID={UNIT_TESTS_USER};Password={UNIT_TESTS_PASSWORD}";
            var pg = new PostgresProvider(str);
            using var connection = pg.CreateConnection();
            var result = await pg.IsReadWriteConnectionAsync(connection);
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task ReadonlyConnectionStringTest()
        {
            var str = "Host=srvk2788,srvd4905;Port=5432;Database=smartmonitoring;Pooling=true;User ID={SM_PG_USER};Password={SM_PG_PASSWORD}";
            var pg = new PostgresProvider(str);
            using var connection = pg.CreateConnection();
            var result = await pg.IsReadWriteConnectionAsync(connection);
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task PrimaryConnectionStringTest()
        {
            var str = "Host=srvk2788,srvd4905;Port=5432;Database=smartmonitoring;Pooling=true;User ID={SM_PG_USER};Password={SM_PG_PASSWORD};Target Session Attributes=primary";
            var pg = new PostgresProvider(str);
            using var connection = pg.CreateConnection();
            var result = await pg.IsReadWriteConnectionAsync(connection);
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task BulkInsertTest()
        {
            var pg = new PostgresProvider(context.ConnectionString("postgres"));
            var mappings = new Dictionary<string, string>
            {
                //{"Date", "dt" },
                {"Level", "level" },
                //{"Source", "source" },
                //{"Message", "message" },
                //{"Exception", "exception" },
                //{"Server", "host" },
                //{"App", "app" }
            };
            var values = new DbEvent[]
            {
               new DbEvent
               {
                   Date = DateTime.Now,
                   Level = 1
               },
               new DbEvent
               {
                   Date = DateTime.Now,
                   Level = 2
               },
            };

            var tableName = "log.logs";
            using var connection = pg.CreateConnection();
            await connection.ExecuteAsync($"truncate table {tableName}");
            await pg.BulkInsertAsync(connection,
                data: values,
                tableName: tableName,
                batchSize: 2,
                mappings: mappings);
        }

        private class DbEvent
        {
            public DateTime Date { get; set; } = DateTime.Now;
            public int Level { get; set; }
            public string? Source { get; set; }
            public string? Message { get; set; }
            public string? Exception { get; set; }
            public string? Server { get; set; } = Environment.MachineName;

            public string? App => Assembly.GetEntryAssembly().GetName().Name
                        + ":"
                        + Assembly.GetEntryAssembly().GetName().Version.ToString();
        }
    }
}