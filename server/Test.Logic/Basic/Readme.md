# Structure of basic tests

> These lists are not exhaustive. It is enough to have *some* tests listed for
> each function to ensure full coverage.

Each group tests a specific part of the `w5logic` language and the core systems.

- `BasicRoles`: Checks if we can transition between phases and have a basic
  voting system setup.
- `Filter`: Checks filter operations
- `LabelVisibility`: Checks if the visibility settings of label works.
- `RandomStuff`: Checks any rng related functions
- `SimpleGeneration`: Check if code generation works at all and we get usuable
  output.

The following language functions are tested in ...

| Name | Group |
|-|-|
| `... \| filter` | `Filter`, `LabelVisibility` |
| `... \| get` | `LabelVisibility` |
| `... \| has` | `Filter` |
| `... \| has_character` | `Filter` |
| `... \| has_not` | `Filter` |
| `... \| has_not_character` | `Filter` |
| `add` | `BasicRoles`, `Filter`, `LabelVisibility`, `RandomStuff` |
| `empty` | `Filter` |
| `get` | `LabelVisibility`, `RandomStuff` |
| `get_with` | `LabelVisibility` |
| `get2` | `RandomStuff` |
| `has` | `Filter` |
| `has_character` | `Filter` |
| `has_not` | `Filter` |
| `has_not_character` | `Filter` |
| `labels` | `LabelVisibility` |
| `length` | `Filter`, `RandomStuff` |
| `rand` | `RandomStuff` |
| `set_invisible` | `LabelVisibility` |
| `set_visible` | `LabelVisibility` |
| `shuffle` | `RandomStuff` |
| `split` | `RandomStuff` |

The following language statements are tested in ...

| Name | Group |
|-|-|
| `for let ...` | `RandomStuff` |
| `if let ...` | `BasicRoles`, `LabelVisibility`, `RandomStuff` |
| `spawn voting ...` | `BasicRoles` |
| `spawn voting ... with choices` | `BasicRoles` |
