using System.Net;
using System.Text.Json.Nodes;
using NUnit.Framework;
using Quvantic.FhirApiTests.Support;
using Reqnroll;

namespace Quvantic.FhirApiTests.Steps;

[Binding]
public sealed class SearchSteps(ScenarioState state)
{
    public static List<JsonNode> Entries(JsonNode? bundle, string resourceType, string mode) =>
        (bundle?["entry"]?.AsArray() ?? [])
            .Where(e => e!["resource"]!["resourceType"]!.GetValue<string>() == resourceType
                        && (e["search"]?["mode"]?.GetValue<string>() ?? "match") == mode)
            .Select(e => e!["resource"]!)
            .ToList();

    private static string Given(JsonNode patient) => patient["name"]![0]!["given"]![0]!.GetValue<string>();

    [Given("these patients exist with a unique shared family name:")]
    public async Task GivenPatients(DataTable table)
    {
        state.Family = "Fam" + Guid.NewGuid().ToString("N")[..12];
        foreach (var row in table.Rows)
        {
            var mrn = Resources.UniqueValue("mrn");
            var created = await state.Client.PostAsync("Patient",
                Resources.Patient(mrn, state.Family, row["given"], row["birthDate"], row["gender"]));
            CommonSteps.AssertOk(created, HttpStatusCode.Created);
            state.PatientsByGiven[row["given"]] = (created.Body!["id"]!.GetValue<string>(), mrn);
        }
    }

    [When("I search for the patient {string} by MRN")]
    public async Task WhenSearchByMrn(string given) =>
        state.Last = await state.Client.GetAsync($"Patient?identifier={Resources.IdentifierSystem}|{state.PatientsByGiven[given].Mrn}");

    [Then("the search returns {int} patient named {string}")]
    public void ThenOnePatient(int count, string given)
    {
        var patients = Entries(state.Response.Body, "Patient", "match");
        Assert.That(patients, Has.Count.EqualTo(count));
        Assert.That(patients.Select(Given), Is.All.EqualTo(given));
    }

    [When("I search the family for birthdate {string}")]
    public async Task WhenBirthdate(string filter) =>
        state.Last = await state.Client.GetAsync($"Patient?family={state.Family}&birthdate={filter}");

    [When("I search the family for gender {string} and birthdate {string}")]
    public async Task WhenGenderBirthdate(string gender, string filter) =>
        state.Last = await state.Client.GetAsync($"Patient?family={state.Family}&gender={gender}&birthdate={filter}");

    [Then("the search returns these given names: {}")]
    public void ThenNames(string names)
    {
        CommonSteps.AssertOk(state.Response, HttpStatusCode.OK);
        var expected = names.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var actual = Entries(state.Response.Body, "Patient", "match").Select(Given);
        Assert.That(actual, Is.EquivalentTo(expected));
    }

    [Then("the search returns these given names:")]
    public void ThenNoNames() => ThenNames("");

    [When("I search the family with _count {int}")]
    public async Task WhenCount(int count) =>
        state.Last = await state.Client.GetAsync($"Patient?family={state.Family}&_count={count}&_sort=birthdate");

    private string? Link(string relation) =>
        state.Response.Body?["link"]?.AsArray()
            .FirstOrDefault(l => l!["relation"]!.GetValue<string>() == relation)?["url"]?.GetValue<string>();

    [Then("the page has {int} patient(s) and a {string} link")]
    public void ThenPageWithLink(int count, string relation)
    {
        Assert.That(Entries(state.Response.Body, "Patient", "match"), Has.Count.EqualTo(count));
        Assert.That(Link(relation), Is.Not.Null);
    }

    [Then("the page has {int} patient(s) and no {string} link")]
    public void ThenPageWithoutLink(int count, string relation)
    {
        Assert.That(Entries(state.Response.Body, "Patient", "match"), Has.Count.EqualTo(count));
        Assert.That(Link(relation), Is.Null);
    }

    [When("I follow the {string} link")]
    public async Task WhenFollow(string relation)
    {
        var url = Link(relation) ?? throw new AssertionException($"no {relation} link");
        // Paging links are absolute and use the server's own idea of its address; keep only the query.
        var query = new Uri(url).Query;
        state.Last = await state.Client.GetAsync(query.Length > 0 ? query : url);
    }

    [Given("{string} has a heart-rate observation of {int} bpm")]
    public async Task GivenObservation(string given, int bpm)
    {
        var created = await state.Client.PostAsync("Observation", Resources.HeartRate(state.PatientsByGiven[given].Id, bpm));
        CommonSteps.AssertOk(created, HttpStatusCode.Created);
    }

    [When("I search heart-rate observations for {string} including the subject")]
    public async Task WhenSearchObservations(string given) =>
        state.Last = await state.Client.GetAsync(
            $"Observation?subject=Patient/{state.PatientsByGiven[given].Id}&code=http://loinc.org|8867-4&_include=Observation:subject");

    [Then("the bundle contains {int} {string} with search mode {string}")]
    public void ThenBundleContains(int count, string type, string mode) =>
        Assert.That(Entries(state.Response.Body, type, mode), Has.Count.EqualTo(count), state.Response.Body?.ToJsonString());
}
