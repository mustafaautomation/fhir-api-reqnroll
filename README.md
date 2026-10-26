# fhir-api-reqnroll

[![CI](https://github.com/mustafaautomation/fhir-api-reqnroll/actions/workflows/ci.yml/badge.svg)](https://github.com/mustafaautomation/fhir-api-reqnroll/actions/workflows/ci.yml)

BDD API tests for an HL7 FHIR R4 server, written with [Reqnroll](https://reqnroll.net/) on .NET 10.
Each run starts a real [HAPI FHIR](https://hapifhir.io/) server in Docker through Testcontainers, so
the tests need no shared environment and leave nothing behind.

Personal portfolio project, not production or client work. All patient data is synthetic.

## What the scenarios cover (21)

| Feature | What is checked |
|---|---|
| Server capabilities | `/metadata` is a CapabilityStatement for FHIR 4.0.1, supports JSON, and Patient supports read, vread, create, update, delete, search-type and history |
| Patient lifecycle | Create returns 201 with `Location: Patient/{id}/_history/1` and `ETag: W/"1"`; read returns the stored data with `meta.versionId` and `meta.lastUpdated`; update with `If-Match` creates version 2; a stale `If-Match` is rejected and changes nothing; `_history` lists versions newest first and old versions stay readable (vread); delete gives 410 Gone, not 404; unknown ids give 404 with an OperationOutcome |
| Conditional create | `If-None-Exist: identifier=system\|value` returns 201 the first time and 200 with the same patient the second time, so a retried request cannot create a duplicate |
| Search | Token search on identifier; `birthdate` with `lt`, `ge`, `eq`, `gt` prefixes (scenario outline); combined parameters behave as AND; `_count` paging follows `next` links to the end; `_include=Observation:subject` returns the patient with search mode `include` next to the matching heart-rate Observation (LOINC 8867-4) |
| Validation | Broken JSON, the wrong resource type for the endpoint, a non-ISO date and an invalid gender code all return 400 with an OperationOutcome that explains the problem |

Scenarios are independent: each creates its own data with unique identifiers and family names, so
they can run in any order against a server that already has data in it.

### A spec deviation the tests pin down

The FHIR R4 spec says a version mismatch on `If-Match` should return **412 Precondition Failed**.
HAPI FHIR 8.12 returns **409 Conflict**. The stale-update scenario (tagged `@hapi-deviation`)
asserts 409, which is what the server really does, and the feature file says why. If HAPI changes,
that scenario fails and the difference is noticed rather than hidden behind "409 or 412".

## How it is built

- `FhirClient` is a thin `HttpClient` wrapper that sends and receives `application/fhir+json` and
  exposes status, headers (`ETag`, `Location`) and the body as `JsonNode`. No FHIR SDK is used, so
  every header and field the tests rely on is visible in the code.
- `FhirServer` starts `hapiproject/hapi:v8.12.0-2` once per test run (`[BeforeTestRun]`) and waits
  for `/fhir/metadata` to answer before any scenario runs. Set `FHIR_BASE_URL` to run the same
  scenarios against another R4 server instead.
- Step classes share per-scenario state through Reqnroll's context injection.
- Missing or pending steps fail the run (`reqnroll.json`), and the build treats warnings as errors.
- Reqnroll writes an HTML report and a Cucumber messages file to `bin/<config>/net10.0/reports/`;
  CI uploads them with the TRX results.

## Run it

Requires the .NET 10 SDK and Docker.

```
dotnet test
```

Without Docker you can point the suite at any FHIR R4 server you are allowed to write test data to:

```
FHIR_BASE_URL=https://hapi.fhir.org/baseR4 dotnet test
```

All 21 scenarios passed against HAPI's public test server that way in October 2026 (it is a shared
public server, so expect it to be slower and occasionally unavailable).

## Limits

- Only Patient and Observation, and only the REST API. No SMART on FHIR / OAuth, no profiles
  (US Core, IPS) and no `$validate` against a profile.
- HAPI's default configuration is used. Production FHIR servers often disable or restrict delete,
  history and some search parameters, so several scenarios document behaviour that varies by server.

## License

MIT
