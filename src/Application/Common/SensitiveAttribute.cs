namespace Application.Common;

/// <summary>
/// Marks a property as containing sensitive data (e.g. passwords, tokens, secrets).
///
/// When a type carrying this attribute is destructured by Serilog, the marked
/// properties are replaced with <c>***</c> in all log sinks (Console, File, Seq, etc.).
/// Apply to any command or DTO property that must never appear in plain text in logs.
/// </summary>
/// <example>
/// <code>
/// public class CreateUserCommand : IRequest&lt;UserDto&gt;
/// {
///     public required string Email    { get; set; }
///     [Sensitive] public required string Password { get; set; }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class SensitiveAttribute : Attribute { }
