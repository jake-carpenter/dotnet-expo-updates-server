using System.Text.Json.Serialization;

namespace ExpoUpdatesServer;

public class Metadata
{
    public int Version { get; init; }
    public string? Bundler { get; init; }
    public FileMetadata? FileMetadata { get; init; }
}

public class FileMetadata
{
    public Platform? Android { get; init; }
    public Platform? Ios { get; init; }
}

public class Platform
{
    public string? Bundle { get; init; }
    public List<Asset>? Assets { get; init; }
}

public class Asset
{
    public string? Path { get; init; }
    public string? Ext { get; init; }

    [JsonIgnore]
    public string? Filename => Path?.Split('/').Last();
}