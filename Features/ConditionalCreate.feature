Feature: Conditional create
  Retried or duplicated create requests must not produce duplicate patients.

  Scenario: If-None-Exist on the identifier returns the existing patient instead of a duplicate
    Given a new patient "Okafor", "Chidi" with a unique MRN
    When I create the patient only if none exists with that MRN
    Then the response status is 201
    When I create the patient only if none exists with that MRN
    Then the response status is 200
    And the response refers to the same patient as the first create
    And searching by the MRN finds exactly 1 patient
