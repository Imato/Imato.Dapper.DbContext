namespace Imato.Dapper.DbContext.Test.Exceptions
{
    public class SlqInjectionTests
    {
        private void AssertSql(string sql)
        {
            var result = SlqInjection.Check(sql);
            Assert.That(result, Is.EqualTo(sql));
        }

        [Test]
        public void Select_Test_False()
        {
            AssertSql("select * from same_table order by 1; execute dbo.same_pricedure");
        }

        [Test]
        public void Create_Test_True()
        {
            var sql = "create table same_table(id int);";
            Assert.Throws<SqlInjectionException>(() => SlqInjection.Check(sql));
        }

        [Test]
        public void Create_Test_False()
        {
            AssertSql("create table #same_table(id int);");
        }
    }
}