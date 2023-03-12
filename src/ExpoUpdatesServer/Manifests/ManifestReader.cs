using System.Text.Json;
using FluentResults;

namespace ExpoUpdatesServer.Manifests;

public class MetadataReader
{
    private readonly string _runtimeVersion;

    private static readonly JsonSerializerOptions JsonOptions = new()
        { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public MetadataReader(string runtimeVersion)
    {
        _runtimeVersion = runtimeVersion;
        MetadataFile = new FileInfo($"updates/{runtimeVersion}/metadata.json");
    }

    public FileInfo MetadataFile { get; }

    public async Task<Result<Metadata>> ReadContents()
    {
        try
        {
            var metadataContents = await File.ReadAllBytesAsync($"updates/{_runtimeVersion}/metadata.json");
            var metadata = JsonSerializer.Deserialize<Metadata>(metadataContents, JsonOptions);

            return metadata is null ? Result.Fail("Failed to parse metadata.json") : Result.Ok(metadata);
        }
        catch
        {
            return Result.Fail("Failed to parse metadata.json");
        }
    }
}