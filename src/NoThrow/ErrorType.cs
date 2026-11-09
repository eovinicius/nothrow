namespace NoThrow;

/// <summary>
/// The category of an <see cref="Error"/>. Integrations translate it into their own vocabulary,
/// e.g. an HTTP status code.
/// </summary>
/// <remarks>
/// <para>
/// Values from 1000 upwards are reserved for categories defined by the application, for outcomes the members below
/// do not cover. Declare them once, as constants:
/// </para>
/// <code>
/// public static class AppErrorTypes
/// {
///     public const ErrorType RateLimited = (ErrorType)1000;
/// }
///
/// var error = new Error("Api.RateLimited", "Too many requests.", AppErrorTypes.RateLimited);
/// </code>
/// <para>
/// Any other value that is not a member is rejected when an <see cref="Error"/> is created. New members are only ever
/// added below 1000, so application-defined values never collide with them. An integration that meets a value it does
/// not know should treat it as <see cref="Failure"/>, or let the application map it.
/// </para>
/// </remarks>
public enum ErrorType
{
    /// <summary>A generic failure that fits no other category.</summary>
    Failure,

    /// <summary>The input is invalid.</summary>
    Validation,

    /// <summary>The requested resource does not exist.</summary>
    NotFound,

    /// <summary>The caller is not authenticated.</summary>
    Unauthorized,

    /// <summary>The caller is authenticated but not allowed to perform the operation.</summary>
    Forbidden,

    /// <summary>The operation conflicts with the current state of the resource.</summary>
    Conflict
}
