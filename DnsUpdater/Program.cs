public static class Program
{
    private static void Main()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        app.MapGet("/", () => "Hello World!");

        app.Run();
    }
}
