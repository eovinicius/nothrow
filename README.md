# NoThrow

[![NuGet](https://img.shields.io/nuget/v/NoThrow)](https://www.nuget.org/packages/NoThrow)
[![CI](https://img.shields.io/github/actions/workflow/status/eovinicius/nothrow/ci.yml?branch=main&label=CI)](https://github.com/eovinicius/nothrow/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue)](https://github.com/eovinicius/nothrow/blob/main/LICENSE)

A lightweight, dependency-free **Result pattern** for .NET.

Errors are values, not exceptions: every operation returns a result that either succeeded (optionally with a value) or failed with a typed `Error`, so failure shows up in the signature instead of in a `throw`.

```sh
dotnet add package NoThrow
```

Targets the supported LTS releases: `net8.0` and `net10.0`.

## Errors

An `Error` has a stable `Code` that callers branch on, a human-readable `Message`, and an `ErrorType`
(`Failure`, `Validation`, `NotFound`, `Unauthorized`, `Forbidden`, `Conflict`).

```csharp
public static class UserErrors
{
    public static readonly Error NotFound = Error.NotFound("User.NotFound", "User not found.");
    public static readonly Error EmailTaken = Error.Conflict("User.EmailTaken", "Email is already in use.");
    public static readonly Error Inactive = Error.Conflict("User.Inactive", "User is inactive.");
}
```

When no category fits, define your own from `1000` upwards; that range is reserved for applications, and any other
value outside the list above is rejected:

```csharp
public static class ApiErrors
{
    public const ErrorType RateLimited = (ErrorType)1000;

    public static readonly Error TooManyRequests = new("Api.TooManyRequests", "Too many requests.", RateLimited);
}
```

Per-field failures are aggregated into a single `ValidationError`, so request and domain validation share one shape.
By convention, **a validation error's `Code` is the field name**:

```csharp
return new ValidationError([
    Error.Validation("Email", "Email is required."),
    Error.Validation("Name", "Name is too long."),
]);
```

Value objects follow the same convention, so their failures can be combined later (see `Result.Combine` below):

```csharp
public sealed class Email
{
    private Email(string value) => Value = value;

    public string Value { get; }

    public static Result<Email> Create(string? value) =>
        string.IsNullOrWhiteSpace(value) || !value.Contains('@')
            ? Error.Validation("Email", "Email is invalid.")
            : new Email(value);
}
```

A `ValidationError` requires at least one per-field error and rejects `null` and `Error.None`; a nested
`ValidationError` is flattened into its per-field errors. Two of them are equal when their per-field errors are equal.
Its code is always `Validation.Failed` and its type `Validation`: `with` can change its `Message`, but changing either of
those throws.

## Handling results

Check the result, return early on failure, carry on with the value. `TryGetValue` and `TryGetError` do the check and
the unwrap in one line; `Result.Combine` checks several results at once:

```csharp
public async Task<Result<Guid>> Handle(PlaceOrder command)
{
    var email = Email.Create(command.Email);
    var quantity = Quantity.Create(command.Quantity);

    if (Result.Combine(email, quantity).TryGetError(out var error))
        return error;                                  // a ValidationError listing every invalid field

    var customer = await _customers.FindAsync(command.CustomerId);
    if (customer.IsFailure)
        return customer.Error;                         // Error converts into any result type

    if (!Order.Place(customer.Value, email.Value, quantity.Value).TryGetValue(out var order, out error))
        return error;                                  // reuse `error`: a second `out var error` does not compile

    if (customer.Value.Reserve(order.Total).TryGetError(out error))
        return error;

    await _orders.AddAsync(order);
    return order.Id;                                   // the value converts into a successful result
}
```

`Result.Combine` inspects every result instead of stopping at the first failure. Validation failures are merged into
a single `ValidationError`; any other failure wins, in argument order. Reading `Value` on a failure throws, so read it
only after the check; `ValueOrDefault` never throws.

## Creating results

Both values and errors convert implicitly, so methods read naturally:

```csharp
public Result<User> Find(Guid id)
{
    var user = _users.FirstOrDefault(u => u.Id == id);

    if (user is null)
        return UserErrors.NotFound;

    return user;
}

public Result Rename(string name) =>
    string.IsNullOrWhiteSpace(name)
        ? Error.Validation("Name", "Name is required.")
        : Result.Success();
```

`Result.Create` turns a possibly-`null` value into a result, with the error you choose:

```csharp
public Result<Order> Find(Guid id) => Result.Create(_orders.FindOrDefault(id), OrderErrors.NotFound);
```

## Pitfalls

**Converting a possibly-`null` value.** `return _users.FirstOrDefault(...);` raises a nullable warning (CS8604):
say what a missing value means with `Result.Create(value, error)`. A `null` that still reaches the conversion becomes
`Error.NullValue`, a generic `Failure`. `Result.Success(null)` throws.

**A second `out var error`.** `out var` declarations in an `if` condition belong to the enclosing scope, so declaring
`error` twice is CS0128. Declare it once, then reuse it with `out error`, as in the handler above.

**Comparing errors with `==`.** Errors are records, so `==` also compares `Message`; a message with details in it never
matches. Compare by code and type instead:

```csharp
if (result.Error.Is(UserErrors.NotFound))
    return NotFound();
```

**`ValueOrDefault` on value types.** It returns `default` on failure, so a failed `Result<int>` yields `0`, the same as
a success holding `0`. Check `IsSuccess` (or use `TryGetValue`) when `default` is a valid value.

**`Task.FromResult(error)`.** It produces a `Task<Error>`, which does not convert into `Task<Result<T>>`. Name the type,
or make the method `async`:

```csharp
public Task<Result<User>> FindAsync(Guid id) => Task.FromResult<Result<User>>(UserErrors.NotFound);
```

**Interfaces.** C# forbids user-defined conversions from an interface type, so when the expression's static type is an
interface (e.g. an `IReadOnlyList<Order>` variable), return `Result.Success(list)` instead of `list`.

**Returning or caching a result as JSON.** Serializing a `Result` throws `NotSupportedException`, on success too.
Return a DTO built from the value, or the `Error`; see [Serialization](#serialization).

**Asynchronous side effects.** Use `TapAsync` / `TapErrorAsync`, which await them. Passing an `async` delegate to the
synchronous `Tap` / `TapError` is a compile error, so it can never run as fire-and-forget.

## When not to use Result

Use `Result` for outcomes the caller is expected to handle: invalid input, a missing resource, a conflict, a broken
business rule. Keep exceptions for technical failures and bugs: a database or network outage, a timeout, a `null`
where none can be. Let those propagate to your global handler instead of turning them into `ErrorType.Failure`;
wrapping them hides the stack trace and invites callers to retry things that cannot succeed.

## Composing (optional)

The early-return style above is enough. When a pipeline reads better as a chain, the same operations are available
as combinators:

```csharp
Result<Guid> result = Find(userId)
    .Ensure(user => user.IsActive, UserErrors.Inactive)   // fails when the rule does not hold
    .Bind(user => _orders.Create(user, items))            // runs only on success
    .Map(order => order.Id)                               // transforms the value
    .Tap(id => _log.Info($"Order {id} created"))          // side effect on success
    .TapError(error => _log.Warn(error.Code))             // side effect on failure
    .MapError(error => OrderErrors.CreationFailed);       // translates the error

string text = result.Match(
    onSuccess: id => $"Order {id} created",
    onFailure: error => error.Message);
```

`BindAsync`, `MapAsync`, `EnsureAsync`, `MatchAsync`, `TapAsync` and `TapErrorAsync` take asynchronous delegates on a
result. On a `Task` of a result, every composition method, `MapErrorAsync` included, takes a synchronous delegate,
and all but `MapErrorAsync` also take an asynchronous one. A chain mixing sync and async steps needs a single `await`:

```csharp
string text = await _users.FindAsync(userId)                 // Task<Result<User>>
    .EnsureAsync(user => user.IsActive, UserErrors.Inactive)
    .BindAsync(user => _orders.CreateAsync(user, items))     // async step
    .MapAsync(order => order.Id)                             // sync step
    .TapAsync(async id => await _bus.PublishAsync(new OrderCreated(id)))   // awaited side effect
    .MatchAsync(id => $"Order {id} created", error => error.Message);
```

## Testing

No helper package is needed. Asserting on the error rather than on `IsSuccess` makes a failing test say why:

```csharp
var result = await handler.Handle(command);

Assert.Equal(Error.None, result.Error);   // fails with: Error { Code = User.NotFound, Type = NotFound }
```

To expect a specific failure, compare by code and type:

```csharp
Assert.True(result.Error.Is(UserErrors.NotFound));
```

## Logging

`ToString()` prints `Success` or `Failure(Code)` and never the value, so a result formatted into a log message
(`{Result}`) cannot leak the data it carries. The debugger still shows the value.

That guarantee covers `ToString()` only. Structured loggers that destructure objects (Serilog's `{@Result}`) and
reflection-based serializers such as Newtonsoft.Json read the public properties instead, `Value` and `Error.Message`
included. Don't destructure results; log `result.Error.Code`.

An `Error` prints its code and type but never its message (`Error { Code = User.EmailTaken, Type = Conflict }`),
and a `ValidationError` lists only the field names. Log the `Code`, which is stable and safe, and keep user input
out of `Message`: write `"Email is already in use."`, not `$"Email {email} is already in use."`.

## Serialization

A result is control flow, not a data contract. `System.Text.Json` refuses to convert one in either direction, with a
`NotSupportedException` that says what to do: serialized as-is, a failure would throw from `Value` and a success
would leak the value. Unwrap it into your own type at the boundary:

```csharp
public sealed record UserResponse(Guid Id, string Name);

string json = result.Match(
    onSuccess: user => JsonSerializer.Serialize(new UserResponse(user.Id, user.Name)),
    onFailure: error => JsonSerializer.Serialize(error));
```

Errors are data. An `Error` is written as `{"Code":"User.NotFound","Message":"User not found.","Type":2}` and reads
back. A `ValidationError` adds its per-field `Errors`, even where the declared type is `Error`; to read one back with
them, deserialize it as `ValidationError`. An error type derived by your application is written as a plain `Error`.
All of this works with source-generated `JsonSerializerContext`s, so it is safe for trimming and Native AOT.

## Contributing

Bug reports, ideas and pull requests are welcome. Read the
[contributing guide](https://github.com/eovinicius/nothrow/blob/main/CONTRIBUTING.md) before opening a pull request,
ask questions in [Discussions](https://github.com/eovinicius/nothrow/discussions), and report security issues
privately as described in the [security policy](https://github.com/eovinicius/nothrow/blob/main/SECURITY.md).

## License

[MIT](https://github.com/eovinicius/nothrow/blob/main/LICENSE)
