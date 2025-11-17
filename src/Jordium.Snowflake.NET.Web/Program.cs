namespace Jordium.Snowflake.NET.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 使用 Startup.cs 中的 ConfigureServices
            var startup = new Startup(builder.Configuration, builder.Environment);
            startup.ConfigureServices(builder.Services);            

            var app = builder.Build();

            // 使用 Startup.cs 中的 Configure
            startup.Configure(app, app.Environment);

            app.Run();
        }
    }
}
