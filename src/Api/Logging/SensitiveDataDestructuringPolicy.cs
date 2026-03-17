using System.Collections.Concurrent;
using System.Reflection;
using Application.Common;
using Serilog.Core;
using Serilog.Events;

namespace Api.Logging;

/// <summary>
/// Serilog <see cref="IDestructuringPolicy"/> that masks properties annotated with
/// <see cref="SensitiveAttribute"/> by replacing their values with <c>***</c>.
///
/// Behaviour:
/// - Inspects each object Serilog attempts to destructure (via <c>{@obj}</c> tokens).
/// - If the type has at least one <see cref="SensitiveAttribute"/> property, all public
///   instance properties are emitted as a <see cref="StructureValue"/>, with sensitive
///   ones replaced by the mask constant.
/// - Types without any sensitive property are skipped immediately (returns <c>false</c>)
///   so Serilog applies its default destructuring — zero overhead for non-sensitive types.
/// - Type metadata is cached in a <see cref="ConcurrentDictionary{TKey,TValue}"/> to
///   avoid repeated reflection on hot log paths.
///
/// Registration (Program.cs):
/// <code>
/// builder.Services.AddSerilog((services, config) =>
///     config.Destructure.With&lt;SensitiveDataDestructuringPolicy&gt;()
///           .ReadFrom.Configuration(...));
/// </code>
/// </summary>
public sealed class SensitiveDataDestructuringPolicy : IDestructuringPolicy
{
    private const string Mask = "***";

    // null entry = type has no sensitive properties → skip destructuring
    private static readonly ConcurrentDictionary<Type, TypeMetadata?> Cache = new();

    /// <inheritdoc />
    public bool TryDestructure(
        object value,
        ILogEventPropertyValueFactory propertyValueFactory,
        out LogEventPropertyValue result)
    {
        var metadata = Cache.GetOrAdd(value.GetType(), BuildMetadata);

        if (metadata is null)
        {
            result = null!;
            return false;
        }

        var logProperties = metadata.AllProperties.Select(p =>
        {
            var logValue = metadata.SensitiveProperties.Contains(p)
                ? (LogEventPropertyValue)new ScalarValue(Mask)
                : propertyValueFactory.CreatePropertyValue(p.GetValue(value), destructureObjects: true);

            return new LogEventProperty(p.Name, logValue);
        }).ToList();

        result = new StructureValue(logProperties);
        return true;
    }

    private static TypeMetadata? BuildMetadata(Type type)
    {
        var allProps = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var sensitiveProps = allProps
            .Where(p => p.IsDefined(typeof(SensitiveAttribute), inherit: true))
            .ToHashSet();

        return sensitiveProps.Count > 0
            ? new TypeMetadata(allProps, sensitiveProps)
            : null;
    }

    private sealed record TypeMetadata(
        PropertyInfo[] AllProperties,
        HashSet<PropertyInfo> SensitiveProperties);
}
