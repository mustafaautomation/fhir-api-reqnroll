using System.Net;
using NUnit.Framework;
using Quvantic.FhirApiTests.Support;
using Reqnroll;

namespace Quvantic.FhirApiTests.Steps;

[Binding]
public sealed class CommonSteps(ScenarioState state)
{
    [When("I read {string}")]
    public async Task WhenIRead(string path) => state.Last = await state.Client.GetAsync(path);

    [When("I post this body to {string}: {}")]
    public async Task WhenIPostRaw(string type, string body) => state.Last = await state.Client.PostRawAsync(type, body);

    [Then("the response status is {int}")]
    public void ThenStatusIs(int status) =>
        Assert.That((int)state.Response.Status, Is.EqualTo(status), state.Response.Body?.ToJsonString());

    [Then("the response is a {string}")]
    public void ThenResponseIs(string resourceType) => Assert.That(state.Response.ResourceType, Is.EqualTo(resourceType));

    [Then("the response is an OperationOutcome with severity {string}")]
    public void ThenOperationOutcome(string severity)
    {
        Assert.That(state.Response.ResourceType, Is.EqualTo("OperationOutcome"), state.Response.Body?.ToJsonString());
        var issues = state.Response.Body!["issue"]!.AsArray();
        Assert.That(issues, Is.Not.Empty);
        Assert.That(issues.Select(i => i!["severity"]!.GetValue<string>()), Does.Contain(severity));
        Assert.That(issues.Any(i => i!["diagnostics"] is not null || i!["details"] is not null), "an issue should explain the problem");
    }

    public static void AssertOk(FhirResponse response, params HttpStatusCode[] expected) =>
        Assert.That(expected, Does.Contain(response.Status), response.Body?.ToJsonString());
}
