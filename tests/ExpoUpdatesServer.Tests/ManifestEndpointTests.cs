using System.Net;
using System.Net.Http.Json;
using ExpoUpdatesServer.Manifests;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ExpoUpdatesServer.Tests;

public class ManifestEndpointTests : IClassFixture<Fixture>, IDisposable
{
    private readonly Fixture _fixture;
    private readonly HttpClient _client;

    public ManifestEndpointTests(Fixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    public void Dispose()
    {
        _fixture.Reset();
    }

    [Fact(DisplayName = "HTTP 200 when request is valid")]
    public async Task ValidRequest()
    {
        const string runtimeVersion = "1";
        var request = new HttpRequestMessage(HttpMethod.Get, $"/manifest?platform=ios&runtimeVersion={runtimeVersion}");
        var fakeData = new FakeMetadata(Fixture.UpdatesDirectory, runtimeVersion);
        fakeData.WriteFiles();

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory(DisplayName = "Manifest response includes heading information about the bundle")]
    [InlineData("1", "2020-01-01T01:01:01")]
    [InlineData("1.1", "2023-01-01T01:01:01")]
    public async Task ManifestHeading(string runtimeVersion, string createdDateString)
    {
        var createdDate = DateTime.Parse(createdDateString);
        var fakeData = new FakeMetadata(Fixture.UpdatesDirectory, runtimeVersion);
        fakeData.WriteFiles(createdDate);

        var url = $"/manifest?platform=ios&runtimeVersion={runtimeVersion}";
        var response = await _client.GetFromJsonAsync<Manifest>(url);

        response.ShouldNotBeNull();
        response.CreatedAt.ShouldBe(createdDate);
        response.RuntimeVersion.ShouldBe(runtimeVersion);
    }

    [Theory(DisplayName = "Manifest response includes all assets in the bundle for the requested platform")]
    [InlineData("ios", "android")]
    [InlineData("android", "ios")]
    public async Task Assets(string requestedPlatform, string otherPlatform)
    {
        const string runtimeVersion = "1";
        var fakeData = new FakeMetadata(Fixture.UpdatesDirectory, runtimeVersion);

        fakeData.AddAsset("png", platform: requestedPlatform);
        fakeData.AddAsset("png", platform: otherPlatform);
        fakeData.AddAsset("ttf", platform: requestedPlatform);
        fakeData.WriteFiles();

        var url = $"/manifest?platform={requestedPlatform}&runtimeVersion={runtimeVersion}";
        var response = await _client.GetFromJsonAsync<Manifest>(url);

        response.ShouldNotBeNull();
        response.Assets.Count.ShouldBe(2);
        response.Assets[0].FileExtension.ShouldBe(".png");
        response.Assets[1].FileExtension.ShouldBe(".ttf");
    }

    [Fact(DisplayName = "Manifest response includes key of each asset matching the filename")]
    public async Task AssetKey()
    {
        const string runtimeVersion = "1";
        var fakeData = new FakeMetadata(Fixture.UpdatesDirectory, runtimeVersion);

        fakeData.AddAsset("png");
        fakeData.AddAsset("ttf");
        fakeData.WriteFiles();

        const string url = $"/manifest?platform=ios&runtimeVersion={runtimeVersion}";
        var response = await _client.GetFromJsonAsync<Manifest>(url);

        response.ShouldNotBeNull();
        response.Assets.Count.ShouldBe(2);
        response.Assets[0].Key.ShouldBe(fakeData.AssetFiles[0].Filename);
        response.Assets[1].Key.ShouldBe(fakeData.AssetFiles[1].Filename);
    }

    [Fact(DisplayName = "Manifest response includes file extension of each asset")]
    public async Task AssetFileExtension()
    {
        const string runtimeVersion = "1";
        var fakeData = new FakeMetadata(Fixture.UpdatesDirectory, runtimeVersion);

        fakeData.AddAsset("png");
        fakeData.AddAsset("ttf");
        fakeData.WriteFiles();

        const string url = $"/manifest?platform=ios&runtimeVersion={runtimeVersion}";
        var response = await _client.GetFromJsonAsync<Manifest>(url);

        response.ShouldNotBeNull();
        response.Assets.Count.ShouldBe(2);
        response.Assets[0].FileExtension.ShouldBe(fakeData.AssetFiles[0].Extension);
        response.Assets[1].FileExtension.ShouldBe(fakeData.AssetFiles[1].Extension);
    }

    [Theory(DisplayName = "Manifest response includes full URL of each asset")]
    [InlineData("https://foo.bar/ota-files", "1", "https://foo.bar/ota-files/assets?asset=updates/1/assets/")]
    [InlineData(
        "https://devws.mwiah.com/mobile-ota",
        "2.9",
        "https://devws.mwiah.com/mobile-ota/assets?asset=updates/2.9/assets/")]
    public async Task AssetUrl(string baseUrl, string runtimeVersion, string expectedUrlAndQuery)
    {
        _fixture.AppConfig = new Config { BaseUrl = baseUrl };
        var fakeData = new FakeMetadata(Fixture.UpdatesDirectory, runtimeVersion);

        fakeData.AddAsset("png");
        fakeData.AddAsset("ttf");
        fakeData.WriteFiles();

        var url = $"/manifest?platform=ios&runtimeVersion={runtimeVersion}";
        var response = await _client.GetFromJsonAsync<Manifest>(url);

        response.ShouldNotBeNull();
        response.Assets.Count.ShouldBe(2);
        response.Assets[0].Url.ShouldBe($"{expectedUrlAndQuery}{fakeData.AssetFiles[0].Filename}");
        response.Assets[1].Url.ShouldBe($"{expectedUrlAndQuery}{fakeData.AssetFiles[1].Filename}");
    }

    [Fact(DisplayName = "HTTP 400 when neither 'expo-platform' header nor 'platform' param are provided")]
    public async Task NoPlatform()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "manifest");

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorResponse.ShouldNotBeNull().Error.ShouldBe("Unsupported platform. Expected either ios or android.");
    }

    [Theory(DisplayName = "HTTP 400 when no valid platform is passed via headers or query parameters")]
    [InlineData("foo", "bar")]
    [InlineData(null, null)]
    [InlineData("foo", null)]
    [InlineData(null, "bar")]
    public async Task RequiresPlatform(string header, string parameter)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/manifest?platform={parameter}&runtimeVersion=1");
        request.Headers.TryAddWithoutValidation("expo-platform", header);
        request.Headers.TryAddWithoutValidation("expo-runtime-version", "1");

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorResponse.ShouldNotBeNull().Error.ShouldBe("Unsupported platform. Expected either ios or android.");
    }

    [Fact(DisplayName = "HTTP 400 when neither 'expo-runtime-version' header nor 'runtime-version' param are provided")]
    public async Task NoRuntimeVersion()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "manifest?platform=android");

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorResponse.ShouldNotBeNull().Error.ShouldBe("No runtimeVersion provided.");
    }

    [Fact(DisplayName = "HTTP 404 when the runtimeVersion requested does not exist as a directory")]
    public async Task NoRuntimeVersionDirectory()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/manifest");
        request.Headers.Add("expo-platform", "ios");
        request.Headers.Add("expo-runtime-version", "does-not-exist");

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorResponse.ShouldNotBeNull().Error.ShouldBe("No updates for runtimeVersion 'does-not-exist' available.");
    }

    [Fact(DisplayName = "HTTP 500 when metadata.json could not be read")]
    public async Task NoMetadata()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/manifest");
        request.Headers.Add("expo-platform", "ios");
        request.Headers.Add("expo-runtime-version", "1.0");

        // Create files then remove metadata.json
        var fakeData = new FakeMetadata(Fixture.UpdatesDirectory, "1.0");
        fakeData.WriteFiles();
        Fixture.RemoveFile("./updates/1.0/metadata.json");

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);

        var errorResponse = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        errorResponse.ShouldNotBeNull();
        errorResponse.Detail.ShouldBe("Failed to parse metadata.json");
    }

    [Fact(DisplayName = "HTTP 404 when metadata.json can't be parsed")]
    public async Task BadMetadata()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/manifest");
        request.Headers.Add("expo-platform", "ios");
        request.Headers.Add("expo-runtime-version", "1.0");

        // Create metadata.json that won't parse correctly
        var fakeData = new FakeMetadata(Fixture.UpdatesDirectory, "1.0");
        fakeData.WriteFiles();
        await Fixture.WriteFile("./updates/1.0/metadata.json", "bad json");

        var response = await _client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);

        var errorResponse = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        errorResponse.ShouldNotBeNull();
        errorResponse.Detail.ShouldBe("Failed to parse metadata.json");
    }
}