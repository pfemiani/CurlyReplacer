# CurlyReplacer
CurlyReplacer parses and replaces `{{ ... }}` blocks in strings.
It also includes `CurlyExpressionReplacer`, which evaluates the block contents as runtime expressions using `System.Linq.Dynamic.Core`.

## Project structure
- `src/CurlyReplacer/Utilities/CurlyReplacer.cs`
  - low-level capture parser and replacer
- `src/CurlyReplacer/Utilities/CurlyExpressionReplacer.cs`
  - expression-driven replacement extensions
- `tests/CurlyReplacer.Tests/CurlyReplacerTests.cs`
  - parser/replacer and expression behavior tests

## CurlyReplacer usage
`CurlyReplacer` is useful when you want deterministic token replacement without expression evaluation.

### Parse captures
`CurlyReplacer.Parse(input)` returns `IEnumerable<CurlyCapture>`.
Each capture includes:
- `Content` - text inside the outer `{{` and `}}`
- `StartIndex` - index of the first `{` in the opening `{{`
- `EndIndex` - index of the last `}` in the closing `}}`

Nested `{{` / `}}` inside a capture are treated as part of that same capture.

### Replace captures
`CurlyReplacer.Replace(input, replacement)` replaces each top-level capture using the provided delegate.

Example:
- Input: `Hello {{name}} from {{city}}`
- Delegate: `s => s.ToUpperInvariant()`
- Output: `Hello NAME from CITY`

Behavior notes:
- if the replacement delegate returns `null`, it is treated as an empty string
- unmatched opening `{{` does not produce a capture and leaves trailing input unchanged
- inputs with no `{{` token return quickly without capture/replacement work

## CurlyExpressionReplacer usage
`CurlyExpressionReplacer` adds extension methods on `string`:
- `input.ReplaceCurlyExpressions(context)`
- `input.ReplaceCurlyExpressions()`

For each `{{ ... }}` block, the content is parsed as a Dynamic LINQ expression and evaluated at runtime.

### With context
You can reference context properties directly:
- Input: `Hello {{FirstName}}`
- Context: object with `FirstName = "Pete"`
- Output: `Hello Pete`

You can also use composed expressions:
- `{{FirstName + " " + LastName + " is " + Age}}`
- `{{Age >= 21 ? "adult" : "minor"}}`

### Without context
`ReplaceCurlyExpressions()` evaluates expressions using an empty context object.
This is useful for constants and arithmetic:
- Input: `Value: {{1 + (2 * 5)}}`
- Output: `Value: 11`

### Method calls and Dynamic LINQ accessibility
Method calls are subject to `System.Linq.Dynamic.Core` method accessibility rules.
If a method call is not allowed by Dynamic LINQ, evaluation throws `InvalidOperationException` with an inner parse/evaluation exception.

In this repository's sample program (`src/CurlyReplacer/Program.cs`), the context type is annotated with `[DynamicLinqType]` to allow calling:
- `{{Test(1, 2, 3)}}`

## Performance notes
- `CurlyReplacer.Parse` and `CurlyReplacer.Replace` perform a fast check for `{{` and return immediately for non-template strings.
- `CurlyExpressionReplacer` caches compiled Dynamic LINQ delegates by context type and expression text, so repeated expressions avoid repeated parse/compile overhead.
- `ReplaceCurlyExpressions()` reuses a shared empty context object to avoid repeated per-call allocation.

## Running
Build:
- `dotnet build CurlyReplacer.sln`

Run sample app:
- `dotnet run --project src/CurlyReplacer/CurlyReplacer.csproj`

Run tests:
- `dotnet test tests/CurlyReplacer.Tests/CurlyReplacer.Tests.csproj`
