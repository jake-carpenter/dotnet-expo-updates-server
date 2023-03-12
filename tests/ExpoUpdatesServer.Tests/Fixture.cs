using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ExpoUpdatesServer.Tests;

public class Fixture : WebApplicationFactory<Program>
{
    public static readonly DirectoryInfo UpdatesDirectory = new("./updates");

    public Config? AppConfig { get; set; }

    public Fixture()
    {
        Reset().GetAwaiter().GetResult();
    }

    public Task Reset()
    {
        ClearFiles();
        AppConfig = new Config { BaseUrl = "https://default.base.url/created-in-fixture" };
        return Task.CompletedTask;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // This overrides the setup done in Program.cs and configures DI for IOptions<Config>.
        // By doing this, we maintain this instance as the injected one, allowing config values
        // to change in the test then be reset between tests.
        builder.ConfigureTestServices(services => { services.AddScoped(_ => Options.Create(AppConfig)); });
    }

    private static void ClearFiles()
    {
        if (UpdatesDirectory.Exists)
        {
            UpdatesDirectory.Delete(recursive: true);
        }
    }

    public static void RemoveFile(string path)
    {
        var file = new FileInfo(path);
        file.Delete();
    }

    public static async Task WriteFile(string path, string content)
    {
        await File.WriteAllTextAsync(path, content);
    }
}