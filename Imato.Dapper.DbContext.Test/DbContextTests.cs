using System.Data;

namespace Imato.Dapper.DbContext.Test
{
    public class DbContextTests
    {
        private DbContext context;

        private TestClass[] values = Enumerable.Range(-100, 201)
            .Select(x => new TestClass
            {
                id = x,
                name = $"Test id = {x}",
                date = new DateTime(2023, 10, 1, 11, 11, 45).AddSeconds(x * 1000)
            })
            .ToArray();

        [OneTimeSetUp]
        public void Setup()
        {
            AppBulder.SetupApp();
            context = AppBulder.GetRequiredService<DbContext>();
        }

        [Test]
        public void ConnectionStringTest()
        {
            var method = AppBulder.GetMethod<DbContext>("ConnectionString");
            var result = method?.Invoke(context, new object[] { "" });
            Assert.That(result.ToString().Contains("Host=localhost"));
        }

        [Test]
        public void ConnectionTest()
        {
            var parameters = new object[] { "", "", "" };
            var method = AppBulder.GetMethod<DbContext>("Connection", parameters);
            var result = method?.Invoke(context, parameters) as IDbConnection;
            Assert.That(result != null);
            Assert.That(result.ConnectionString.Contains("User"));
            Assert.That(result.ConnectionString.Contains("Password"));
            Assert.That(result.ConnectionString.Contains("Host"));
        }

        [Test]
        public void IsMasterServerTest()
        {
            Assert.False(context.IsMasterServer());
        }

        [Test]
        public void IsDbActiveTest()
        {
            Assert.True(context.IsDbActive());
        }

        [Test]
        public async Task CreateTestTableTest()
        {
            var tableName = Model.GetTable<TestClass>();
            var name = $"Create {tableName}";
            context.AddCommand(new ContextCommand
            {
                ContextVendor = ContextVendors.postgres,
                Name = name,
                Text = $"create table if not exists {tableName} (id int not null primary key, name varchar(255) not null, date timestamp);"
            });
            await context.ExecuteAsync(name);
        }

        [Test]
        public async Task ClearTest()
        {
            await CreateTestTableTest();
            await context.TruncateAsync<TestClass>();
            var result = await context.GetAllAsync<TestClass>();
            Assert.That(result.Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task InsertAsyncTest()
        {
            await CreateTestTableTest();
            await context.TruncateAsync<TestClass>();
            await context.InsertAsync(values.First());
            var result = await context.GetAllAsync<TestClass>();
            Assert.That(result.Count(), Is.EqualTo(1));
            Assert.That(result.First().id, Is.EqualTo(-100));
        }

        [Test]
        public async Task DeleteAsyncTest()
        {
            await InsertAsyncTest();
            await context.DeleteAsync(values.First());
            var result = await context.GetAllAsync<TestClass>();
            Assert.That(result.Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task UpdateAsyncTest()
        {
            await InsertAsyncTest();
            var t = values.First();
            t.name = "Case Update";
            await context.UpdateAsync(t);
            var result = await context.GetAllAsync<TestClass>();
            Assert.That(result.First().name, Is.EqualTo(t.name));
        }

        [Test]
        public async Task InsertManyTest()
        {
            await ClearTest();
            await context.InsertAsync(values);
            var result = await context.GetAllAsync<TestClass>();
            Assert.That(result.Count(), Is.EqualTo(201));
            var resultLast = result.Last();
            var valueLast = values.Last();
            Assert.That(resultLast.name, Is.EqualTo(valueLast.name));
            Assert.That(resultLast.date, Is.EqualTo(valueLast.date));
        }

        [Test]
        public async Task BulkInsertAsyncTest()
        {
            await ClearTest();
            await context.BulkInsertAsync(values);
            var result = await context.GetAllAsync<TestClass>();
            Assert.That(result.Count(), Is.EqualTo(201));
            Assert.That(result.Last().name, Is.EqualTo(values.Last().name));
        }

        [Test]
        public void GetValuesOfTest()
        {
            var value = values.First();
            var result = BulkCopy.GetValuesOf(value);
            Assert.That(result.Count(), Is.EqualTo(3));
            Assert.IsTrue(result.ContainsKey("date"));

            result = BulkCopy.GetValuesOf(value, "Id,Name,Date,Value".Split(","));
            Assert.That(result.Count(), Is.EqualTo(3));
            Assert.IsTrue(result.ContainsKey("Date"));
            Assert.IsFalse(result.ContainsKey("Value"));
            Assert.That(result["Id"], Is.EqualTo(-100));
        }

        [Test]
        public void GetColumnsOfTest()
        {
            var result = BulkCopy.GetColumnsOf<TestClass>();
            Assert.That(string.Join(",", result.Values), Is.EqualTo("date,id,name"));

            var columns = "Name,Value";
            result = BulkCopy.GetColumnsOf<TestClass>(columns.Split(","));
            Assert.That(string.Join(",", result.Values), Is.EqualTo(columns));
        }
    }
}