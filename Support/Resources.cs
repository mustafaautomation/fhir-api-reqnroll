using System.Text.Json.Nodes;

namespace Quvantic.FhirApiTests.Support;

/// <summary>Builders for the FHIR R4 JSON the scenarios send.</summary>
public static class Resources
{
    /// <summary>Identifier system for test data. Each scenario uses unique values so runs never collide.</summary>
    public const string IdentifierSystem = "urn:quvantic:test:mrn";

    public static string UniqueValue(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..(prefix.Length + 13)];

    public static JsonObject Patient(string mrn, string family, string given, string birthDate = "1985-04-12", string gender = "female") => new()
    {
        ["resourceType"] = "Patient",
        ["identifier"] = new JsonArray(new JsonObject { ["system"] = IdentifierSystem, ["value"] = mrn }),
        ["name"] = new JsonArray(new JsonObject { ["family"] = family, ["given"] = new JsonArray(given) }),
        ["gender"] = gender,
        ["birthDate"] = birthDate,
    };

    /// <summary>A heart-rate Observation (LOINC 8867-4) for the given patient.</summary>
    public static JsonObject HeartRate(string patientId, int beatsPerMinute) => new()
    {
        ["resourceType"] = "Observation",
        ["status"] = "final",
        ["category"] = new JsonArray(new JsonObject
        {
            ["coding"] = new JsonArray(new JsonObject
            {
                ["system"] = "http://terminology.hl7.org/CodeSystem/observation-category",
                ["code"] = "vital-signs",
            }),
        }),
        ["code"] = new JsonObject
        {
            ["coding"] = new JsonArray(new JsonObject { ["system"] = "http://loinc.org", ["code"] = "8867-4", ["display"] = "Heart rate" }),
        },
        ["subject"] = new JsonObject { ["reference"] = $"Patient/{patientId}" },
        ["effectiveDateTime"] = "2026-10-01T09:30:00Z",
        ["valueQuantity"] = new JsonObject
        {
            ["value"] = beatsPerMinute,
            ["unit"] = "beats/minute",
            ["system"] = "http://unitsofmeasure.org",
            ["code"] = "/min",
        },
    };
}
