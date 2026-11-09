using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Serialization;

namespace NoThrow;

/// <summary>
/// Describes why an operation failed.
/// </summary>
/// <param name="Code">A stable, machine-readable identifier (e.g. <c>"User.NotFound"</c>) that callers can branch on.</param>
/// <param name="Message">A human-readable description of the failure.</param>
/// <param name="Type">
/// The category of the failure: a member of <see cref="ErrorType"/>, or an application-defined value from 1000 upwards.
/// </param>
/// <remarks>
/// <para>
/// Not sealed so that <see cref="ValidationError"/> can extend it.
/// <see cref="ToString"/> prints <see cref="Code"/> and <see cref="Type"/> but never <see cref="Message"/>,
/// so logging an error cannot leak data embedded in its message.
/// </para>
/// <para>
/// An error is a data contract: <c>System.Text.Json</c> writes it as <c>{"Code":…,"Message":…,"Type":…}</c> and reads
/// it back. Declared as <see cref="Error"/>, a <see cref="ValidationError"/> also writes its <c>Errors</c> (it reads
/// back as a plain <see cref="Error"/>; declare <see cref="ValidationError"/> to keep them), and an error type derived
/// by the application is written as an <see cref="Error"/>.
/// </para>
/// </remarks>
/// <exception cref="ArgumentNullException"><paramref name="Code"/> or <paramref name="Message"/> is <see langword="null"/>.</exception>
/// <exception cref="ArgumentOutOfRangeException">
/// <paramref name="Type"/> is neither a member of <see cref="ErrorType"/> nor an application-defined value (1000 and above).
/// </exception>
[JsonPolymorphic(UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor)]
[JsonDerivedType(typeof(ValidationError))]
[SuppressMessage(
    "Naming",
    "CA1716:Identifiers should not match keywords",
    Justification = "Error is the library's central type; it only clashes with a Visual Basic keyword, which VB escapes as [Error].")]
public record Error(string Code, string Message, ErrorType Type)
{
    // ErrorType values from here upwards are reserved for application-defined categories.
    private const int ApplicationTypeMinimum = 1000;

    private readonly string _code = Code ?? throw new ArgumentNullException(nameof(Code));
    private readonly string _message = Message ?? throw new ArgumentNullException(nameof(Message));
    private readonly ErrorType _type = ValidateType(Type);

    /// <summary>The absence of an error. Carried by every successful <see cref="Result"/>.</summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    /// <summary>
    /// Produced when a <see langword="null"/> value is converted into a <see cref="Result{TValue}"/>.
    /// A generic <see cref="ErrorType.Failure"/>: an unhandled <see langword="null"/> is a bug, not invalid input.
    /// </summary>
    public static readonly Error NullValue = new("Error.NullValue", "A null value was provided.", ErrorType.Failure);

    /// <summary>A stable, machine-readable identifier (e.g. <c>"User.NotFound"</c>) that callers can branch on.</summary>
    /// <exception cref="ArgumentNullException">The value is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The error is a <see cref="ValidationError"/>, whose code cannot change.</exception>
    public string Code
    {
        get => _code;
        init
        {
            ArgumentNullException.ThrowIfNull(value, nameof(Code));

            if (HasFixedCodeAndType && !string.Equals(value, _code, StringComparison.Ordinal))
            {
                throw new ArgumentException($"The code of a {nameof(ValidationError)} is always '{_code}'.", nameof(Code));
            }

            _code = value;
        }
    }

    /// <summary>A human-readable description of the failure. Keep user input out of it.</summary>
    /// <exception cref="ArgumentNullException">The value is <see langword="null"/>.</exception>
    public string Message
    {
        get => _message;
        init => _message = value ?? throw new ArgumentNullException(nameof(Message));
    }

    /// <summary>The category of the failure.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value is neither a member of <see cref="ErrorType"/> nor an application-defined value (1000 and above).
    /// </exception>
    /// <exception cref="ArgumentException">The error is a <see cref="ValidationError"/>, whose type cannot change.</exception>
    public ErrorType Type
    {
        get => _type;
        init
        {
            ValidateType(value);

            if (HasFixedCodeAndType && value != _type)
            {
                throw new ArgumentException($"The type of a {nameof(ValidationError)} is always {_type}.", nameof(Type));
            }

            _type = value;
        }
    }

    // ValidationError fixes its code and type; `with` and object initializers must not change them.
    // Only the init accessors read it, so it is never called before construction completes.
    private protected virtual bool HasFixedCodeAndType => false;

    /// <summary>Creates an error of type <see cref="ErrorType.Failure"/>.</summary>
    public static Error Failure(string code, string message) => new(code, message, ErrorType.Failure);

    /// <summary>Creates an error of type <see cref="ErrorType.Validation"/>.</summary>
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    /// <summary>Creates an error of type <see cref="ErrorType.NotFound"/>.</summary>
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    /// <summary>Creates an error of type <see cref="ErrorType.Unauthorized"/>.</summary>
    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);

    /// <summary>Creates an error of type <see cref="ErrorType.Forbidden"/>.</summary>
    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);

    /// <summary>Creates an error of type <see cref="ErrorType.Conflict"/>.</summary>
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    /// <summary>
    /// Whether <paramref name="other"/> identifies the same failure: same <see cref="Code"/> (ordinal) and
    /// <see cref="Type"/>, whatever the <see cref="Message"/>. Equality (<c>==</c>) still compares every member.
    /// </summary>
    /// <example>
    /// <code>
    /// if (result.Error.Is(UserErrors.NotFound))
    ///     return NotFound();
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is <see langword="null"/>.</exception>
    public bool Is(Error other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return string.Equals(Code, other.Code, StringComparison.Ordinal) && Type == other.Type;
    }

    /// <summary>Prints <see cref="Code"/> and <see cref="Type"/>; <see cref="Message"/> is left out because it may carry user data.</summary>
    protected virtual bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Code = ").Append(Code).Append(", Type = ").Append(Type);

        return true;
    }

    // A range check rather than Enum.IsDefined: no cost on every error created. A test checks that it accepts every
    // member, so a member added after Conflict fails it until the upper bound here is updated.
    [SuppressMessage(
        "Usage",
        "CA2208:Instantiate argument exceptions correctly",
        Justification = "The callers are the constructor and the init accessor; to them the argument is named Type.")]
    private static ErrorType ValidateType(ErrorType type) =>
        type is >= ErrorType.Failure and <= ErrorType.Conflict || (int)type >= ApplicationTypeMinimum
            ? type
            : throw new ArgumentOutOfRangeException(
                nameof(Type),
                type,
                $"Not an {nameof(ErrorType)} member, nor an application-defined value ({ApplicationTypeMinimum} and above).");
}
