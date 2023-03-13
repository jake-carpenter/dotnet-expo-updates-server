namespace ExpoUpdatesServer.Manifests;

public class ManifestAsset
{
    public ManifestAsset()
    {
        // This constructor will be used in test setup.
    }

    public ManifestAsset(string runtimeVersion, Asset asset, Config config)
    {
        Key = asset.Filename;
        FileExtension = $".{asset.Ext}";
        Url = $"{config.BaseUrl}/assets?asset=updates/{runtimeVersion}/{asset.Path}";
    }

    public string? Hash { get; init; }
    public string? Key { get; init; }
    public string? FileExtension { get; init; }
    public string? ContentType { get; init; }
    public string? Url { get; init; }
}