using ExpoUpdatesServer;
using ExpoUpdatesServer.Manifests;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<Config>(builder.Configuration);

var app = builder.Build();
app.UseHttpsRedirection();

// Endpoints
app.MapGet("/manifest", ManifestEndpoint.Handle);

app.Run();

// For integration tests
// ReSharper disable once ClassNeverInstantiated.Global
public partial class Program
{
}