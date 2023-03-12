var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();
app.UseHttpsRedirection();
app.Run();

// For integration tests
// ReSharper disable once ClassNeverInstantiated.Global
public partial class Program
{
}
