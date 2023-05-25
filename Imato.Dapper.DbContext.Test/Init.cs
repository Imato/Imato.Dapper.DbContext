namespace Imato.Dapper.DbContext.Test
{
    [SetUpFixture]
    public class Init
    {
        public static ContextFactory Factory;

        [OneTimeSetUp]
        public void Setup()
        {
            Program.Main(Array.Empty<string>());
            Factory = Program.Factory;
        }
    }
}