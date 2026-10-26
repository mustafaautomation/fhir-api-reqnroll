Feature: Patient lifecycle
  Create, read, update, version and delete a Patient over the FHIR REST API.

  Background:
    Given a new patient "Rivera", "Ana" with a unique MRN

  Scenario: Create returns 201 with the new location and version 1
    When I create the patient
    Then the response status is 201
    And the Location header points to the new patient at version 1
    And the ETag is version 1

  Scenario: Read returns what was stored, plus server metadata
    Given the patient has been created
    When I read the patient
    Then the response status is 200
    And the patient has family name "Rivera" and the MRN
    And the resource has meta.versionId "1" and a meta.lastUpdated timestamp

  Scenario: An update with the current version in If-Match creates version 2
    Given the patient has been created
    When I change the family name to "Rivera-Lopez" with If-Match for version 1
    Then the response status is 200
    And the ETag is version 2
    And reading the patient returns family name "Rivera-Lopez"

  # The R4 spec says a version mismatch on If-Match is 412 Precondition Failed. HAPI FHIR answers
  # 409 Conflict instead. This scenario pins HAPI's actual behaviour so a change is noticed; a client
  # written against this server must treat both codes as "someone else updated it first".
  @hapi-deviation
  Scenario: An update with a stale version is rejected and changes nothing
    Given the patient has been created
    And the family name has been changed to "Rivera-Lopez"
    When I change the family name to "Stale" with If-Match for version 1
    Then the response status is 409
    And the response is an OperationOutcome with severity "error"
    And reading the patient returns family name "Rivera-Lopez"

  Scenario: History lists every version, newest first, and old versions stay readable
    Given the patient has been created
    And the family name has been changed to "Rivera-Lopez"
    When I request the patient's history
    Then the response is a "Bundle" of type "history" with 2 entries
    And version 1 of the patient still has family name "Rivera"

  Scenario: A deleted patient is gone, not missing
    Given the patient has been created
    When I delete the patient
    Then the response status is 200
    And reading the patient returns status 410

  Scenario: An unknown id is 404 with an OperationOutcome
    When I read "Patient/does-not-exist-0000"
    Then the response status is 404
    And the response is an OperationOutcome with severity "error"
