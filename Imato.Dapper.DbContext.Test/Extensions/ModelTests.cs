using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Imato.Dapper.DbContext.Test
{
    public class ModelTests
    {
        [Test]
        public void GetTableTest()
        {
            var result = Model.GetTable<TestClass>();
            Assert.That(result, Is.EqualTo("test_case_1"));
        }
    }
}