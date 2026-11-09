using System.Text.Json.Serialization;

namespace NoThrow.Tests;

// Source-generated metadata: the path trimmed and Native AOT applications use, so the serialization tests
// cover it as well as reflection.
[JsonSerializable(typeof(Result))]
[JsonSerializable(typeof(Result<int>))]
[JsonSerializable(typeof(ResultHolder))]
[JsonSerializable(typeof(Error))]
[JsonSerializable(typeof(ValidationError))]
internal sealed partial class TestJsonContext : JsonSerializerContext
{
}

// A DTO that (wrongly) carries a result, as an API response or a cache entry might.
public sealed record ResultHolder(Result<int> Result);
