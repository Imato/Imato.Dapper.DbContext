using System.ComponentModel.DataAnnotations.Schema;

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

        [Test]
        public void GetDbTest()
        {
            var result = DbAttribute.RequiredValue<TestClass>();
            Assert.That(result, Is.EqualTo("unit_tests"));
        }

        [Test]
        public void GetConnectionTest()
        {
            var result = ConnectionAttribute.RequiredValue<TestClass>();
            Assert.That(result, Is.EqualTo("postgres"));
        }
    }
}