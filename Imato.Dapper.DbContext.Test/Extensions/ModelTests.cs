namespace Imato.Dapper.DbContext.Test
{
    public class ModelTests
    {
        [Test]
        public void GetTableTest()
        {
            var result = TableAttributeExtensions.RequiredValue<TestClass>();
            Assert.That(result, Is.EqualTo("test_case_1"));
        }
    }
}