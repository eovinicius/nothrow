# Changelog

All notable changes to NoThrow are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project uses
[Semantic Versioning](https://semver.org/spec/v2.0.0.html). While the version is `0.x`, a minor release may contain
breaking changes; they are always listed under **Breaking**.

## [Unreleased]

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
