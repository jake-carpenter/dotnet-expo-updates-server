using FluentResults;
using Microsoft.Extensions.Options;

namespace ExpoUpdatesServer.Manifests;

public record struct ManifestParameters(string Platform, string RuntimeVersion);

public static class ManifestEndpoint
{
    public static async Task<IResult> Handle(HttpContext context, IOptions<Config> options)
    {
        var parametersResult = ParseParameters(context);
        if (parametersResult.IsFailed)
            return Results.BadRequest(new ErrorResponse(parametersResult.Errors[0].Message));

        var (platform, runtimeVersion) = parametersResult.Value;
        var runtimeDirectoryInfo = new DirectoryInfo($"updates/{runtimeVersion}");
        if (!runtimeDirectoryInfo.Exists)
            return Results.NotFound(new ErrorResponse($"No updates for runtimeVersion '{runtimeVersion}' available."));

        var metadataReader = new MetadataReader(runtimeVersion);
        var metadataResult = await metadataReader.ReadContents();
        if (metadataResult.IsFailed)
            return Results.Problem(metadataResult.Errors[0].Message);

        return Results.Ok(
            new Manifest
            {
                CreatedAt = metadataReader.MetadataFile.CreationTime,
                RuntimeVersion = runtimeVersion,
                Assets = GetAssetsForPlatform(metadataResult.Value, platform)
                    .Select(asset => new ManifestAsset(runtimeVersion, asset, options.Value))
                    .ToArray()
            });
    }

    private static string? ParseQueryOrHeaderValues(HttpContext context, string queryKey, string headerKey)
    {
        if (!context.Request.Query.TryGetValue(queryKey, out var stringValues))
        {
            stringValues = context.Request.Headers[headerKey];
        }

        return stringValues.FirstOrDefault();
    }

    private static Result<ManifestParameters> ParseParameters(HttpContext context)
    {
        const string platformQueryKey = "platform";
        const string platformHeaderKey = "expo-platform";
        const string runtimeVersionQueryKey = "runtimeVersion";
        const string runtimeVersionHeaderKey = "expo-runtime-version";
        const string platformErrorMsg = "Unsupported platform. Expected either ios or android.";
        const string runtimeVersionErrorMsg = "No runtimeVersion provided.";

        // Require 'ios' or 'android' for platform
        var platform = ParseQueryOrHeaderValues(context, platformQueryKey, platformHeaderKey);
        if (platform is null || (platform != "ios" && platform != "android"))
            return Result.Fail(platformErrorMsg);

        var runtimeVersion = ParseQueryOrHeaderValues(context, runtimeVersionQueryKey, runtimeVersionHeaderKey);
        if (runtimeVersion is not { Length: > 0 })
            return Result.Fail(runtimeVersionErrorMsg);

        var parameters = new ManifestParameters(platform, runtimeVersion);
        return Result.Ok(parameters);
    }

    private static IEnumerable<Asset> GetAssetsForPlatform(Metadata metadata, string platform)
    {
        return platform switch
        {
            "ios" => metadata.FileMetadata!.Ios!.Assets!,
            "android" => metadata.FileMetadata!.Android!.Assets!,
            _ => throw new ArgumentOutOfRangeException(nameof(platform))
        };
    }
}