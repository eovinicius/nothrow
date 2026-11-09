# Contributing to NoThrow

Thanks for your interest in NoThrow! Bug reports, documentation fixes, and code contributions are all welcome.

By participating you agree to follow the [Code of Conduct](CODE_OF_CONDUCT.md).

## Before you start

- **Questions** go to [Discussions](https://github.com/eovinicius/nothrow/discussions), not issues.
- **Bugs**: search the [existing issues](https://github.com/eovinicius/nothrow/issues) first, then open one with the
  bug report template. A minimal snippet that reproduces the problem is the most useful thing you can include.
- **New features and public API changes**: open an issue to discuss the idea **before** writing the code. NoThrow
  keeps a deliberately small surface, so not every useful addition fits; agreeing on the design first saves you from
  writing a pull request that can't be merged.
- **Security vulnerabilities**: never in a public issue. See [SECURITY.md](SECURITY.md).

Small fixes (typos, docs, tests, obvious bugs) can go straight to a pull request.

## Development setup

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download) (the line pinned by `global.json`). To run the tests
on both target frameworks you also need the .NET 8 runtime.

```sh
dotnet build                     # builds the library and the tests (net8.0 + net10.0)
dotnet test                      # runs every test on net8.0 and net10.0
dotnet test -f net10.0           # if you don't have the .NET 8 runtime
dotnet pack src/NoThrow -c Release   # also runs package validation (API-break detection)
```

Benchmarks live in `benchmarks/NoThrow.Benchmarks` (BenchmarkDotNet, not run by `dotnet test`):

```sh
dotnet run -c Release --project benchmarks/NoThrow.Benchmarks -- --filter "*" --job short
```

## Guidelines

- **No dependencies.** The `NoThrow` package must stay dependency-free. Only the benchmarks project may reference a
  third-party package.
- **Warnings are errors.** Every project builds with `TreatWarningsAsErrors`, and every public member under `src/`
  needs an XML doc comment (CS1591 fails the build).
- **Trim/AOT safe.** No reflection: the trim and AOT analyzers are enabled.
- **Tests.** Every behavior change comes with tests. Use xUnit with plain `Assert.*` (no assertion libraries), one test
  class per public type, and name tests `Member_ExpectedBehavior` (e.g. `Failure_WithErrorNone_Throws`).
- **Docs.** `README.md` is also the NuGet package readme. When public behavior changes, update it, including the
  *Pitfalls* and *Testing* sections. Its snippets must compile on `net8.0` with nullable enabled.
- **Breaking changes.** `dotnet pack` compares the public API against the latest published version and fails on an
  undeclared break. Breaking changes need prior discussion in an issue.

## Pull requests

1. Fork the repository and create a branch from `main`.
2. Make your change, with tests, and run `dotnet build` and `dotnet test` locally.
3. Open a pull request and fill in the template. Link the issue it resolves (`Closes #123`).

Every pull request needs:

- a green **CI** run (build, tests on both target frameworks, and pack);
- an approving review from a maintainer, with every review conversation resolved;
- a branch that is up to date with `main`.

Pull requests are **squash-merged**, so the pull request title becomes the commit message on `main`. Write it in the
[Conventional Commits](https://www.conventionalcommits.org) style used by the project history:

```
feat: add Result.Try for wrapping exception-throwing calls
fix: keep the error code in Value's exception message
docs: clarify ValueOrDefault on value types
```

Mark breaking changes with `!` (`feat!: …`). Workflows from first-time and outside contributors need a maintainer's
approval before they run, so CI may not start immediately on your first pull request.

## License

NoThrow is licensed under the [MIT License](LICENSE). By submitting a contribution, you agree that it is licensed under
the same terms.
