namespace Imato.Dapper.DbContext.Test
{
    public class ContextFactoryTests
    {
        [Test]
        public void GetDbContextTests()
        {
            AssertContext("mssql");
            AssertContext("postgres");
            AssertContext("mysql");
            AssertContext("test_mssql");
            AssertContext();
        }

        private void AssertContext(string? name = null)
        {
            var context = name == null
                ? Init.Factory.GetDbContext()
                : Init.Factory.GetDbContext(name);

            Assert.IsNotNull(context);
            Assert.That(context.IsActive);

            if (name != null)
            {
                Assert.That(name == context.Name);
            }
            else
            {
                Assert.That("mssql" == context.Name);
            }
        }
    }
}