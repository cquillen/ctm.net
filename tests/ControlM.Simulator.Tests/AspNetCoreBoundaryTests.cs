using System.Reflection;
using ControlM.Simulator.Hosting;
using Shouldly;

namespace ControlM.Simulator.Tests;

// Enforces "ASP.NET Core stays at the edge of the simulator" in docs/conventions.md. It reads signatures only:
// base types, interfaces, fields, properties, events, parameters, return types and attributes. It does not read
// method bodies, so ASP.NET Core used only inside a method body is not caught.
public sealed class AspNetCoreBoundaryTests
{
    private const string _hostingNamespace = "ControlM.Simulator.Hosting";

    private const BindingFlags _declared =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    [Test]
    public void Only_endpoints_and_the_hosting_area_have_asp_net_core_in_their_signatures()
    {
        var violations = typeof(ControlMSimulator).Assembly.GetTypes()
            .Where(type => !MayUseAspNetCore(type))
            .SelectMany(type => SignatureTypes(type)
                .SelectMany(Expand)
                .Where(IsAspNetCore)
                .Select(used => $"{type.FullName} uses {used.FullName}"))
            .Distinct()
            .ToList();

        violations.ShouldBeEmpty();
    }

    private static bool MayUseAspNetCore(Type type)
    {
        var outermost = type;
        while (outermost.DeclaringType is not null)
        {
            outermost = outermost.DeclaringType;
        }

        var ns = outermost.Namespace ?? string.Empty;
        return outermost.Name.EndsWith("Endpoint", StringComparison.Ordinal)
            || ns == _hostingNamespace
            || ns.StartsWith(_hostingNamespace + ".", StringComparison.Ordinal);
    }

    private static IEnumerable<Type> SignatureTypes(Type type)
    {
        IEnumerable<Type?> types =
        [
            type.BaseType,
            .. type.GetInterfaces(),
            .. AttributeTypes(type),
            .. type.GetFields(_declared).SelectMany(f => AttributeTypes(f).Append(f.FieldType)),
            .. type.GetProperties(_declared).SelectMany(p => AttributeTypes(p).Append(p.PropertyType)),
            .. type.GetEvents(_declared).SelectMany(e => AttributeTypes(e).Append(e.EventHandlerType)),
            .. type.GetMethods(_declared).SelectMany(m => AttributeTypes(m).Append(m.ReturnType).Concat(ParameterTypes(m))),
            .. type.GetConstructors(_declared).SelectMany(c => AttributeTypes(c).Concat(ParameterTypes(c))),
        ];
        return types.OfType<Type>();
    }

    private static IEnumerable<Type> AttributeTypes(MemberInfo member) =>
        member.GetCustomAttributesData().Select(a => a.AttributeType);

    private static IEnumerable<Type> ParameterTypes(MethodBase method) =>
        method.GetParameters().SelectMany(p => p.GetCustomAttributesData().Select(a => a.AttributeType).Append(p.ParameterType));

    private static IEnumerable<Type> Expand(Type type)
    {
        yield return type;
        if (type.HasElementType)
        {
            foreach (var inner in Expand(type.GetElementType()!))
            {
                yield return inner;
            }
        }

        foreach (var argument in type.IsGenericType ? type.GetGenericArguments() : [])
        {
            foreach (var inner in Expand(argument))
            {
                yield return inner;
            }
        }
    }

    private static bool IsAspNetCore(Type type) =>
        type.Assembly.GetName().Name?.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal) == true;
}
