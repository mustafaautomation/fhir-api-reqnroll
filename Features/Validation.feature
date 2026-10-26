Feature: Validation
  Malformed input is rejected with 400 and an OperationOutcome that says why.

  Scenario Outline: Malformed Patient resources are rejected
    When I post this body to "Patient": <body>
    Then the response status is 400
    And the response is an OperationOutcome with severity "<severity>"

    Examples:
      | case                     | body                                                           | severity |
      | not JSON                 | {"resourceType": "Patient", "name": [                          | error    |
      | wrong resource type      | {"resourceType": "Observation", "status": "final"}             | error    |
      | invalid date             | {"resourceType": "Patient", "birthDate": "12/04/1985"}          | error    |
      | invalid gender code      | {"resourceType": "Patient", "gender": "unknown-value"}         | error    |
