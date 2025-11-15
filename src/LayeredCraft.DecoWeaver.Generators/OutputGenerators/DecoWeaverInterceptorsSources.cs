// DecoWeaver/OutputGenerators/DecoWeaverInterceptorsSources.cs

using DecoWeaver.Emit;
using DecoWeaver.Providers;

namespace DecoWeaver.OutputGenerators;

/// <summary>
/// Generates the complete DecoWeaverInterceptors source file using the unified Scriban template.
/// Handles all registration kinds that have been migrated to the template-based approach.
/// </summary>
internal static class DecoWeaverInterceptorsSources
{
    /// <summary>
    /// Model for a single registration passed to the template.
    /// </summary>
    internal readonly record struct RegistrationModel(
        string method_name,
        int method_index,
        string service_fqn,
        string impl_fqn,
        string intercepts_data,
        bool has_decorators,
        string[] decorators,
        bool has_two_type_params,
        bool is_keyed,
        bool has_factory,
        bool has_instance,
        string service_key_param,
        string factory_param,
        string instance_param,
        string keyed_method_name);

    /// <summary>
    /// Generates the complete interceptor file for the given registrations.
    /// </summary>
    internal static string Generate(IEnumerable<RegistrationModel> registrations)
    {
        var model = new { registrations };
        return TemplateHelper.Render(TemplateConstants.DecoWeaverInterceptors, model);
    }

    /// <summary>
    /// Creates a registration model from a ClosedGenericRegistration.
    /// </summary>
    internal static RegistrationModel CreateRegistrationModel(
        ClosedGenericRegistration reg,
        string methodName,
        int methodIndex,
        string[] decorators,
        string escapedInterceptsData)
    {
        return new RegistrationModel(
            method_name: methodName,
            method_index: methodIndex,
            service_fqn: reg.ServiceFqn,
            impl_fqn: reg.ImplFqn,
            intercepts_data: escapedInterceptsData,
            has_decorators: decorators.Length > 0,
            decorators: decorators,
            has_two_type_params: HasTwoTypeParams(reg.Kind),
            is_keyed: IsKeyed(reg.Kind),
            has_factory: HasFactory(reg.Kind),
            has_instance: HasInstance(reg.Kind),
            service_key_param: reg.ServiceKeyParameterName ?? "serviceKey",
            factory_param: reg.FactoryParameterName ?? "implementationFactory",
            instance_param: reg.InstanceParameterName ?? "implementationInstance",
            keyed_method_name: GetKeyedMethodName(methodName));
    }

    private static bool HasTwoTypeParams(RegistrationKind kind) =>
        kind is RegistrationKind.Parameterless
            or RegistrationKind.FactoryTwoTypeParams
            or RegistrationKind.KeyedParameterless
            or RegistrationKind.KeyedFactoryTwoTypeParams;

    private static bool IsKeyed(RegistrationKind kind) =>
        kind is RegistrationKind.KeyedParameterless
            or RegistrationKind.KeyedFactoryTwoTypeParams
            or RegistrationKind.KeyedFactorySingleTypeParam
            or RegistrationKind.KeyedInstanceSingleTypeParam;

    private static bool HasFactory(RegistrationKind kind) =>
        kind is RegistrationKind.FactoryTwoTypeParams
            or RegistrationKind.FactorySingleTypeParam
            or RegistrationKind.KeyedFactoryTwoTypeParams
            or RegistrationKind.KeyedFactorySingleTypeParam;

    private static bool HasInstance(RegistrationKind kind) =>
        kind is RegistrationKind.InstanceSingleTypeParam
            or RegistrationKind.KeyedInstanceSingleTypeParam;

    private static string GetKeyedMethodName(string lifetimeMethod) =>
        lifetimeMethod switch
        {
            "AddTransient" => "AddKeyedTransient",
            "AddScoped" => "AddKeyedScoped",
            _ => "AddKeyedSingleton"
        };
}