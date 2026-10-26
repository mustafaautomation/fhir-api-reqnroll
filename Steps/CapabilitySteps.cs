using NUnit.Framework;
using Quvantic.FhirApiTests.Support;
using Reqnroll;

namespace Quvantic.FhirApiTests.Steps;

[Binding]
public sealed class CapabilitySteps(ScenarioState state)
{
    [When("I request the capability statement")]
    public async Task WhenIRequestCapabilities() => state.Last = await state.Client.GetAsync("metadata");

    [Then("the FHIR version is {string}")]
    public void ThenFhirVersion(string version) =>
        Assert.That(state.Response.Body!["fhirVersion"]!.GetValue<string>(), Is.EqualTo(version));

    [Then("the server supports JSON")]
    public void ThenSupportsJson()
    {
        var formats = state.Response.Body!["format"]!.AsArray().Select(f => f!.GetValue<string>()).ToList();
        Assert.That(formats.Any(f => f.Contains("json")), $"formats: {string.Join(", ", formats)}");
    }

    [Then("the {string} resource supports these interactions:")]
    public void ThenResourceSupports(string type, DataTable table)
    {
        var resource = state.Response.Body!["rest"]![0]!["resource"]!.AsArray()
            .Single(r => r!["type"]!.GetValue<string>() == type);
        var supported = resource!["interaction"]!.AsArray().Select(i => i!["code"]!.GetValue<string>()).ToHashSet();
        Assert.That(supported, Is.SupersetOf(table.Rows.Select(r => r["interaction"])));
    }
}
