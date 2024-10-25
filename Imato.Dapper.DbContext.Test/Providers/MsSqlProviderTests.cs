using Dapper;

namespace Imato.Dapper.DbContext.Test.Providers
{
    public class MsSqlProviderTests
    {
        private DbContext context;
        private MsSqlProvider provider = new();

        [OneTimeSetUp]
        public void Setup()
        {
            AppBulder.SetupApp();
            context = AppBulder.GetRequiredService<DbContext>();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            context.Dispose();
        }

        [Test]
        public void GetReplicaStateTest()
        {
            var cs = context.ConnectionString("sqlcluster");
            var state = provider.GetReplicaState(ref cs);
            Assert.That(state, Is.EqualTo(ReplicaState.ReadWrite));

            cs = context.ConnectionString("sqlcluster_read");
            state = provider.GetReplicaState(ref cs);
            Assert.That(state, Is.EqualTo(ReplicaState.ReadOnly));
            var timeout = provider.GetReplicaStateTimeout(ref cs);
            Assert.That(timeout, Is.EqualTo(TimeSpan.FromSeconds(10)));

            cs = context.ConnectionString("sqlcluster_write");
            state = provider.GetReplicaState(ref cs);
            Assert.That(state, Is.EqualTo(ReplicaState.ReadWrite));
        }

        [Test]
        public async Task GetClusterConnectionTest()
        {
            var sql = "select master.dbo.servername()";

            var connection = context.Connection("sqlcluster");
            var result = await connection.QuerySingleAsync<string>(sql);
            Assert.That(result, Is.EqualTo("SRVD2695"));

            connection = context.Connection("sqlcluster_read");
            result = await connection.QuerySingleAsync<string>(sql);
            Assert.That(result, Is.EqualTo("SRVD6201"));

            connection = context.Connection("sqlcluster_write");
            result = await connection.QuerySingleAsync<string>(sql);
            Assert.That(result, Is.EqualTo("SRVD2695"));

            connection.Dispose();
        }
    }
}