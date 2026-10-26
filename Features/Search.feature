Feature: Search
  Search parameters, prefixes, paging and includes.

  Background:
    Given these patients exist with a unique shared family name:
      | given  | birthDate  | gender |
      | Mei    | 1979-02-03 | female |
      | Tomas  | 1991-07-21 | male   |
      | Priya  | 2001-11-30 | female |

  Scenario: Token search on identifier finds exactly one patient
    When I search for the patient "Tomas" by MRN
    Then the search returns 1 patient named "Tomas"

  Scenario Outline: Date prefixes on birthdate
    When I search the family for birthdate "<filter>"
    Then the search returns these given names: <names>

    Examples:
      | filter       | names        |
      | lt1990-01-01 | Mei          |
      | ge1990-01-01 | Tomas, Priya |
      | eq1991-07-21 | Tomas        |
      | gt2010-01-01 |              |

  Scenario: Combining parameters is an AND
    When I search the family for gender "female" and birthdate "ge1990-01-01"
    Then the search returns these given names: Priya

  Scenario: Paging with _count follows the next link to the end
    When I search the family with _count 2
    Then the page has 2 patients and a "next" link
    When I follow the "next" link
    Then the page has 1 patient and no "next" link

  Scenario: _include brings the subject along with its observations
    Given "Mei" has a heart-rate observation of 72 bpm
    When I search heart-rate observations for "Mei" including the subject
    Then the bundle contains 1 "Observation" with search mode "match"
    And the bundle contains 1 "Patient" with search mode "include"
