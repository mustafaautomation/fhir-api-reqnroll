using System.Net;
using System.Text.Json.Nodes;
using NUnit.Framework;
using Quvantic.FhirApiTests.Support;
using Reqnroll;

namespace Quvantic.FhirApiTests.Steps;

[Binding]
public sealed class PatientSteps(ScenarioState state)
{
    private static string Family(JsonNode? patient) => patient!["name"]![0]!["family"]!.GetValue<string>();

    [Given("a new patient {string}, {string} with a unique MRN")]
    public void GivenANewPatient(string family, string given)
    {
        state.Mrn = Resources.UniqueValue("mrn");
        state.Patient = Resources.Patient(state.Mrn, family, given);
    }

    [When("I create the patient")]
    public async Task WhenICreate()
    {
        state.Last = await state.Client.PostAsync("Patient", state.Patient!);
        state.PatientId = state.Response.Body?["id"]?.GetValue<string>();
    }

    [Given("the patient has been created")]
    public async Task GivenCreated()
    {
        await WhenICreate();
        CommonSteps.AssertOk(state.Response, HttpStatusCode.Created);
    }

    [When("I create the patient only if none exists with that MRN")]
    public async Task WhenConditionalCreate()
    {
        var headers = new Dictionary<string, string> { ["If-None-Exist"] = $"identifier={Resources.IdentifierSystem}|{state.Mrn}" };
        state.Last = await state.Client.PostAsync("Patient", state.Patient!, headers);
        var id = IdFrom(state.Response);
        state.FirstCreatedId ??= id;
        state.PatientId = id;
    }

    [Then("the response refers to the same patient as the first create")]
    public void ThenSamePatient() => Assert.That(state.PatientId, Is.EqualTo(state.FirstCreatedId));

    [Then("searching by the MRN finds exactly {int} patient")]
    public async Task ThenSearchByMrn(int count)
    {
        var bundle = await state.Client.GetAsync($"Patient?identifier={Resources.IdentifierSystem}|{state.Mrn}");
        CommonSteps.AssertOk(bundle, HttpStatusCode.OK);
        Assert.That(SearchSteps.Entries(bundle.Body, "Patient", "match").Count, Is.EqualTo(count));
    }

    [Then("the Location header points to the new patient at version {int}")]
    public void ThenLocation(int version)
    {
        var location = state.Response.Location?.ToString();
        Assert.That(location, Does.Contain($"Patient/{state.PatientId}/_history/{version}"));
    }

    [Then("the ETag is version {int}")]
    public void ThenETag(int version)
    {
        Assert.That(state.Response.ETag, Is.EqualTo($"W/\"{version}\""));
    }

    [When("I read the patient")]
    public async Task WhenIRead() => state.Last = await state.Client.GetAsync($"Patient/{state.PatientId}");

    [Then("the patient has family name {string} and the MRN")]
    public void ThenPatientHas(string family)
    {
        Assert.That(Family(state.Response.Body), Is.EqualTo(family));
        Assert.That(state.Response.Body!["identifier"]![0]!["value"]!.GetValue<string>(), Is.EqualTo(state.Mrn));
    }

    [Then("the resource has meta.versionId {string} and a meta.lastUpdated timestamp")]
    public void ThenMeta(string versionId)
    {
        var meta = state.Response.Body!["meta"]!;
        Assert.That(meta["versionId"]!.GetValue<string>(), Is.EqualTo(versionId));
        Assert.That(DateTimeOffset.TryParse(meta["lastUpdated"]!.GetValue<string>(), out _), "lastUpdated should be a timestamp");
    }

    [When("I change the family name to {string} with If-Match for version {int}")]
    public async Task WhenUpdate(string family, int version)
    {
        var body = (JsonObject)state.Patient!.DeepClone();
        body["id"] = state.PatientId;
        body["name"]![0]!["family"] = family;
        var headers = new Dictionary<string, string> { ["If-Match"] = $"W/\"{version}\"" };
        state.Last = await state.Client.PutAsync($"Patient/{state.PatientId}", body, headers);
    }

    [Given("the family name has been changed to {string}")]
    public async Task GivenChanged(string family)
    {
        await WhenUpdate(family, 1);
        CommonSteps.AssertOk(state.Response, HttpStatusCode.OK);
    }

    [Then("reading the patient returns family name {string}")]
    public async Task ThenReadFamily(string family)
    {
        var read = await state.Client.GetAsync($"Patient/{state.PatientId}");
        CommonSteps.AssertOk(read, HttpStatusCode.OK);
        Assert.That(Family(read.Body), Is.EqualTo(family));
    }

    [When("I request the patient's history")]
    public async Task WhenHistory() => state.Last = await state.Client.GetAsync($"Patient/{state.PatientId}/_history");

    [Then("the response is a {string} of type {string} with {int} entries")]
    public void ThenBundle(string resourceType, string type, int count)
    {
        Assert.That(state.Response.ResourceType, Is.EqualTo(resourceType));
        Assert.That(state.Response.Body!["type"]!.GetValue<string>(), Is.EqualTo(type));
        var entries = state.Response.Body!["entry"]!.AsArray();
        Assert.That(entries, Has.Count.EqualTo(count));
        var versions = entries.Select(e => e!["resource"]!["meta"]!["versionId"]!.GetValue<string>()).ToList();
        Assert.That(versions, Is.EqualTo(Enumerable.Range(1, count).Reverse().Select(v => v.ToString()).ToList()), "newest first");
    }

    [Then("version {int} of the patient still has family name {string}")]
    public async Task ThenVread(int version, string family)
    {
        var old = await state.Client.GetAsync($"Patient/{state.PatientId}/_history/{version}");
        CommonSteps.AssertOk(old, HttpStatusCode.OK);
        Assert.That(Family(old.Body), Is.EqualTo(family));
    }

    [When("I delete the patient")]
    public async Task WhenDelete() => state.Last = await state.Client.DeleteAsync($"Patient/{state.PatientId}");

    [Then("reading the patient returns status {int}")]
    public async Task ThenReadStatus(int status)
    {
        var read = await state.Client.GetAsync($"Patient/{state.PatientId}");
        Assert.That((int)read.Status, Is.EqualTo(status));
    }

    private static string? IdFrom(FhirResponse response)
    {
        if (response.Body?["resourceType"]?.GetValue<string>() == "Patient")
        {
            return response.Body["id"]!.GetValue<string>();
        }
        // Some servers answer a matched conditional create with no body; fall back to Location.
        var segments = response.Location?.ToString().Split('/');
        var i = segments is null ? -1 : Array.IndexOf(segments, "Patient");
        return i >= 0 && i + 1 < segments!.Length ? segments[i + 1] : null;
    }
}
