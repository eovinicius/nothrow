## What and why

<!-- What does this change, and why? Link the issue it resolves, e.g. "Closes #123". -->

## Checklist

- [ ] The PR title follows [Conventional Commits](https://www.conventionalcommits.org) (`feat: …`, `fix: …`, `docs: …`; `!` for breaking changes). It becomes the squash commit message.
- [ ] Tests cover the change, and `dotnet build` and `dotnet test` pass locally.
- [ ] Every new public member has an XML doc comment.
- [ ] `README.md` is updated if public behavior changed (including *Pitfalls* and *Testing*).
- [ ] The `NoThrow` package still has no dependencies.
- [ ] Public API or breaking changes were agreed on in an issue first.
