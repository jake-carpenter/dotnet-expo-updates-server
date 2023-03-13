using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExpoUpdatesServer.Tests;

public record AssetFile(string Filename, string Contents, string Extension);

public class FakeMetadata : Metadata
{
    private readonly DirectoryInfo _runtimeDirectory;

    public FakeMetadata(DirectoryInfo updatesDirectory, string runtimeVersion)
    {
        _runtimeDirectory = new DirectoryInfo(Path.Combine(updatesDirectory.FullName, runtimeVersion));
        Version = 1;
        Bundler = "metro";
        FileMetadata = new FileMetadata
        {
            Android = new Platform
            {
                Bundle = "bundles/android-abcxyz.js",
                Assets = new List<Asset>()
            },
            Ios = new Platform
            {
                Bundle = "bundles/ios-abcxyz11.js",
                Assets = new List<Asset>()
            }
        };
    }

    [JsonIgnore]
    public List<AssetFile> AssetFiles { get; } = new();

    public void AddAsset(string extension, string? contents = null, string? platform = null)
    {
        var fileName = Guid.NewGuid().ToString().Replace("-", string.Empty);
        var asset = new Asset { Ext = extension, Path = $"assets/{fileName}" };
        contents ??= $"fake file {fileName} ({extension})";

        if (platform is null or "android")
        {
            FileMetadata?.Android?.Assets?.Add(asset);
        }

        if (platform is null or "ios")
        {
            FileMetadata?.Ios?.Assets?.Add(asset);
        }

        var assetFile = new AssetFile(fileName, contents, $".{extension}");
        AssetFiles.Add(assetFile);
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

        // Create directories
        var assetsDir = _runtimeDirectory.CreateSubdirectory("assets");
        _runtimeDirectory.CreateSubdirectory("bundle");

        // Write asset files
        foreach (var assetFile in AssetFiles)
        {
            File.WriteAllText($"{assetsDir.FullName}/{assetFile.Filename}", assetFile.Contents);
        }
    }
}