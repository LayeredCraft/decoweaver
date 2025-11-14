// DecoWeaver/Emit/TemplateConstants.cs

namespace DecoWeaver.Emit;

/// <summary>
/// Constants for Scriban template resource paths.
/// Template names follow the format: "Templates.{FileName}.scriban"
/// </summary>
internal static class TemplateConstants
{
    // Common templates
    internal const string InterceptsLocationAttribute = "Templates.Common.InterceptsLocationAttribute.scriban";
    internal const string DecoratorKeys = "Templates.Common.DecoratorKeys.scriban";
    internal const string DecoratorFactory = "Templates.Common.DecoratorFactory.scriban";

    // Registration kind templates
    internal const string ParameterlessInterceptor = "Templates.ParameterlessInterceptor.scriban";
    internal const string FactoryTwoTypeParamsInterceptor = "Templates.FactoryTwoTypeParamsInterceptor.scriban";
    internal const string FactorySingleTypeParamInterceptor = "Templates.FactorySingleTypeParamInterceptor.scriban";
    internal const string KeyedParameterlessInterceptor = "Templates.KeyedParameterlessInterceptor.scriban";
    internal const string KeyedFactoryTwoTypeParamsInterceptor = "Templates.KeyedFactoryTwoTypeParamsInterceptor.scriban";
    internal const string KeyedFactorySingleTypeParamInterceptor = "Templates.KeyedFactorySingleTypeParamInterceptor.scriban";
    internal const string InstanceSingleTypeParamInterceptor = "Templates.InstanceSingleTypeParamInterceptor.scriban";
    internal const string KeyedInstanceSingleTypeParamInterceptor = "Templates.KeyedInstanceSingleTypeParamInterceptor.scriban";
}