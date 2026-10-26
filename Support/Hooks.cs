using Reqnroll;

namespace Quvantic.FhirApiTests.Support;

[Binding]
public sealed class Hooks
{
    [BeforeTestRun]
    public static async Task StartServer() => await FhirServer.StartAsync();

    [AfterTestRun]
    public static async Task StopServer() => await FhirServer.StopAsync();
}

/// <summary>Per-scenario state, shared between step classes by Reqnroll context injection.</summary>
public sealed class ScenarioState : IDisposable
{
    public FhirClient Client { get; } = new(FhirServer.BaseUrl);

    public FhirResponse? Last { get; set; }

    public FhirResponse Response => Last ?? throw new InvalidOperationException("no request has been sent yet");

    public string Mrn { get; set; } = "";

    public System.Text.Json.Nodes.JsonObject? Patient { get; set; }

    public string? PatientId { get; set; }

    public string? FirstCreatedId { get; set; }

    public string Family { get; set; } = "";

    public Dictionary<string, (string Id, string Mrn)> PatientsByGiven { get; } = new();

    public void Dispose() => Client.Dispose();
}
