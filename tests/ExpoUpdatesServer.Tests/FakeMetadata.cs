using System.Text.Json;

namespace ExpoUpdatesServer.Tests;

public class FakeMetadata : Metadata
{
    private readonly DirectoryInfo _runtimeDirectory;

    public FakeMetadata(DirectoryInfo updatesDirectory, string runtimeVersion)
    {
        _runtimeDirectory = new DirectoryInfo(Path.Combine(updatesDirectory.FullName, runtimeVersion));
        Version = 1;
        Bundler = "metro";
    }

    public void WriteFiles(DateTime? createdDate = null)
    {
        // Create runtime version directory
        _runtimeDirectory.Create();

        // Write metadata.json
        var serializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var metadataContents = JsonSerializer.Serialize(this, serializerOptions);

        File.WriteAllText($"{_runtimeDirectory.FullName}/metadata.json", metadataContents);

        // Update creation date for metadata.json
        var metadataFile = new FileInfo($"{_runtimeDirectory.FullName}/metadata.json");
        metadataFile.CreationTime = createdDate ?? DateTime.Parse("2020-01-01T01:01:01");
    }
}