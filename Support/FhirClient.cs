using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;

namespace Quvantic.FhirApiTests.Support;

public sealed record FhirResponse(HttpStatusCode Status, HttpResponseHeaders Headers, HttpContentHeaders? ContentHeaders, JsonNode? Body)
{
    public string? ETag => Headers.ETag?.Tag is { } tag ? (Headers.ETag.IsWeak ? $"W/{tag}" : tag) : null;

    /// <summary>Version id from the ETag (W/"3" gives "3").</summary>
    public string? VersionFromETag => Headers.ETag?.Tag.Trim('"');

    public Uri? Location => Headers.Location ?? ContentLocation;

    private Uri? ContentLocation => ContentHeaders?.ContentLocation;

    public string? ResourceType => Body?["resourceType"]?.GetValue<string>();
}

/// <summary>Thin wrapper over HttpClient that speaks FHIR JSON. No SDK, so every header is visible.</summary>
public sealed class FhirClient : IDisposable
{
    public const string FhirJson = "application/fhir+json";

    private readonly HttpClient _http;

    public FhirClient(Uri baseUrl)
    {
        _http = new HttpClient { BaseAddress = baseUrl, Timeout = TimeSpan.FromSeconds(60) };
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(FhirJson));
    }

    public Task<FhirResponse> GetAsync(string path) => SendAsync(new HttpRequestMessage(HttpMethod.Get, path));

    public Task<FhirResponse> DeleteAsync(string path) => SendAsync(new HttpRequestMessage(HttpMethod.Delete, path));

    public Task<FhirResponse> PostAsync(string path, JsonNode resource, IDictionary<string, string>? headers = null) =>
        SendAsync(WithBody(HttpMethod.Post, path, resource, headers));

    public Task<FhirResponse> PutAsync(string path, JsonNode resource, IDictionary<string, string>? headers = null) =>
        SendAsync(WithBody(HttpMethod.Put, path, resource, headers));

    public Task<FhirResponse> PostRawAsync(string path, string body) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Post, path) { Content = new StringContent(body, Encoding.UTF8, FhirJson) });

    private static HttpRequestMessage WithBody(HttpMethod method, string path, JsonNode resource, IDictionary<string, string>? headers)
    {
        var request = new HttpRequestMessage(method, path)
        {
            Content = new StringContent(resource.ToJsonString(), Encoding.UTF8, FhirJson),
        };
        foreach (var (name, value) in headers ?? new Dictionary<string, string>())
        {
            request.Headers.TryAddWithoutValidation(name, value);
        }
        return request;
    }

    private async Task<FhirResponse> SendAsync(HttpRequestMessage request)
    {
        using (request)
        {
            using var response = await _http.SendAsync(request);
            var text = await response.Content.ReadAsStringAsync();
            var body = string.IsNullOrWhiteSpace(text) ? null : JsonNode.Parse(text);
            return new FhirResponse(response.StatusCode, response.Headers, response.Content.Headers, body);
        }
    }

    public void Dispose() => _http.Dispose();
}
