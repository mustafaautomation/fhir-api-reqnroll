Feature: Server capabilities
  A client should be able to discover what the server supports before relying on it.

  Scenario: The server advertises FHIR R4 and the Patient interactions these tests use
    When I request the capability statement
    Then the response status is 200
    And the response is a "CapabilityStatement"
    And the FHIR version is "4.0.1"
    And the server supports JSON
    And the "Patient" resource supports these interactions:
      | interaction |
      | read        |
      | vread       |
      | create      |
      | update      |
      | delete      |
      | search-type |
      | history-instance |
