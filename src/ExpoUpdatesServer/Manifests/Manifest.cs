namespace ExpoUpdatesServer.Manifests;

public class Manifest
{
    public DateTimeOffset CreatedAt { get; init; }
    public string? RuntimeVersion { get; init; }
    public IList<ManifestAsset> Assets { get; init; } = Array.Empty<ManifestAsset>();
}
