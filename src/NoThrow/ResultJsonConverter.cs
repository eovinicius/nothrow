using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NoThrow;

/// <summary>
/// Makes <c>System.Text.Json</c> reject <see cref="Result"/> and <see cref="Result{TValue}"/>, in both directions,
/// with a <see cref="NotSupportedException"/> that says what to do instead.
/// </summary>
/// <remarks>
/// <para>
/// A result is a control-flow value, not a data contract: serialized as-is, a failure would throw from
/// <see cref="Result{TValue}.Value"/> and a success would leak the value next to an empty error. Unwrap it
/// (<see cref="Result{TValue}.TryGetValue(out TValue)"/>, <see cref="Result{TValue}.Match{TOut}(Func{TValue, TOut}, Func{Error, TOut})"/>)
/// into your own type, or serialize its <see cref="Error"/>, which is a data contract.
/// </para>
/// <para>
/// Applied to both result types through <see cref="JsonConverterAttribute"/>; there is no need to register it.
/// It is public only because the <c>System.Text.Json</c> source generator, used by trimmed and Native AOT
/// applications, must be able to reference it from your assembly.
/// </para>
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class ResultJsonConverter : JsonConverter<Result>
{
    private const string NotSupportedMessage =
        "NoThrow results are not a data contract and cannot be converted to or from JSON. " +
        "Unwrap the result (TryGetValue, Match) into your own type, or serialize its Error.";

    /// <summary>Whether <paramref name="typeToConvert"/> is <see cref="Result"/> or a <see cref="Result{TValue}"/>.</summary>
    public override bool CanConvert(Type typeToConvert) => typeof(Result).IsAssignableFrom(typeToConvert);

    /// <summary>Always throws: results cannot be read from JSON.</summary>
    /// <exception cref="NotSupportedException">Always.</exception>
    public override Result Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw new NotSupportedException(NotSupportedMessage);

    /// <summary>Always throws: results cannot be written to JSON.</summary>
    /// <exception cref="NotSupportedException">Always.</exception>
    public override void Write(Utf8JsonWriter writer, Result value, JsonSerializerOptions options) =>
        throw new NotSupportedException(NotSupportedMessage);
}
