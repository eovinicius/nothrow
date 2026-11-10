# Changelog

All notable changes to NoThrow are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project uses
[Semantic Versioning](https://semver.org/spec/v2.0.0.html). While the version is `0.x`, a minor release may contain
breaking changes; they are always listed under **Breaking**.

## [Unreleased]

### Added

- `ErrorType` values from `1000` upwards are reserved for application-defined categories, e.g.
  `public const ErrorType RateLimited = (ErrorType)1000;`. Built-in members are only ever added below `1000`.

### Changed

- A `ValidationError` serialized with `System.Text.Json` where the declared type is `Error` (e.g. a property of type
  `Error`) now includes its per-field `Errors`. Error types derived by applications are written as a plain `Error`.

### Fixed

- `Result.Combine` returns a lone failing `ValidationError` as is, instead of re-wrapping it into a new one that lost
  its custom message.

### Breaking

- `Result` and `Result<T>` are not a data contract: serializing or deserializing one with `System.Text.Json`
  (reflection or source generation) throws `NotSupportedException` explaining what to do instead. Before, a failure
  threw `InvalidOperationException` from `Value`, and a success wrote the value twice next to an empty error.
  Unwrap results into your own types, or serialize their `Error`.
- Every combinator (`Match`, `Map`, `Bind`, `Ensure`, `Tap`, `TapError`, `MapError`, their `*Async` counterparts)
  and `Result.Create(value, errorIfNull)` throw `ArgumentNullException` for a `null` argument on every call. Before,
  a `null` delegate or error on the branch that did not run went unnoticed.
- The `*Async` methods follow the Task-based Asynchronous Pattern: a `null` argument throws from the call itself, and
  an exception thrown by a delegate, even before it returns a task, surfaces through the returned task. Before,
  `BindAsync` and `MatchAsync` on a `Result`/`Result<T>` let such an exception escape the call, and a `null` argument
  could surface as a `NullReferenceException` when awaited.
- Creating an `Error` (constructor or `with`) with an `ErrorType` value that is neither a member nor `1000` or above
  throws `ArgumentOutOfRangeException`.
- Changing the `Code` or `Type` of a `ValidationError` through `with` or an object initializer throws
  `ArgumentException`; changing its `Message` is still allowed.

## [0.1.0] - 2026-11-08

First release under the NoThrow name. The package was previously published as `Veredito` (`0.1.0-preview.1`),
which is now deprecated in favor of this one.

### Added

- `Result` and `Result<TValue>`: success or failure, with `IsSuccess`/`IsFailure`, `Error`, `Value`,
  `ValueOrDefault`, `TryGetValue` and `TryGetError`.
- Implicit conversions from a value and from an `Error`; `Result.Create` for values that may be `null`.
- `Result.Combine`, which merges validation failures into a single `ValidationError`.
- `Error`, with a stable `Code`, a `Message` and an `ErrorType` (`Failure`, `Validation`, `NotFound`,
  `Unauthorized`, `Forbidden`, `Conflict`), factory methods per type and `Is` to compare by code and type.
- `ValidationError`, which aggregates per-field errors.
- Composition API: `Match`, `Map`, `Bind`, `Ensure`, `Tap`, `TapError`, `MapError` and their `*Async` counterparts,
  on results and on `Task`s of results.
- `ToString` never prints the value of a result or the message of an error.
- Targets `net8.0` and `net10.0`; no dependencies; trimming and Native AOT compatible; SourceLink and symbol package.
