using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Quvantic.FhirApiTests.Support;

/// <summary>
/// One HAPI FHIR server for the whole test run. By default it is started in Docker through
/// Testcontainers; set FHIR_BASE_URL to run the same scenarios against another R4 server.
/// </summary>
public static class FhirServer
{
    public const string Image = "hapiproject/hapi:v8.12.0-2";

    private static IContainer? _container;

    public static Uri BaseUrl { get; private set; } = new("http://localhost:8080/fhir/");

    public static bool IsExternal { get; private set; }

    public static async Task StartAsync()
    {
        var external = Environment.GetEnvironmentVariable("FHIR_BASE_URL");
        if (!string.IsNullOrWhiteSpace(external))
        {
            BaseUrl = new Uri(external.TrimEnd('/') + "/");
            IsExternal = true;
            return;
        }

        _container = new ContainerBuilder(Image)
            .WithPortBinding(8080, true)
            // Delete must be allowed for the delete scenarios (it is, by default); expunge is not needed.
            .WithEnvironment("hapi.fhir.fhir_version", "R4")
            .WithEnvironment("hapi.fhir.allow_multiple_delete", "false")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r
                .ForPort(8080)
                .ForPath("/fhir/metadata")
                .ForStatusCode(System.Net.HttpStatusCode.OK)))
            .Build();

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        await _container.StartAsync(cts.Token);
        BaseUrl = new Uri($"http://{_container.Hostname}:{_container.GetMappedPublicPort(8080)}/fhir/");
    }

    public static async Task StopAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}
