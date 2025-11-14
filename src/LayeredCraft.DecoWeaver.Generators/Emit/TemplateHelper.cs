// DecoWeaver/Emit/TemplateHelper.cs

using System.Reflection;
using Scriban;

namespace DecoWeaver.Emit;

/// <summary>
/// Helper class for loading and validating Scriban templates from embedded resources.
/// </summary>
internal static class TemplateHelper
{
    /// <summary>
    /// Loads a Scriban template from embedded resources.
    /// </summary>
    /// <param name="relativePath">Relative path to the template resource (e.g., "Templates.Common.InterceptsLocationAttribute.scriban")</param>
    /// <returns>Parsed Scriban template ready for rendering</returns>
    /// <exception cref="InvalidOperationException">Thrown if template is not found or has parsing errors</exception>
    internal static Template LoadTemplate(string relativePath)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var baseName = assembly.GetName().Name;

        // Convert relative path to resource name format
        var templateName = relativePath
            .TrimStart('.')
            .Replace(Path.DirectorySeparatorChar, '.')
            .Replace(Path.AltDirectorySeparatorChar, '.');

        // Find the manifest resource name that ends with our template name
        var manifestTemplateName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(x => x.EndsWith(templateName, StringComparison.InvariantCulture));

        if (string.IsNullOrEmpty(manifestTemplateName))
        {
            var availableResources = string.Join(", ", assembly.GetManifestResourceNames());
            throw new InvalidOperationException(
                $"Did not find required resource ending in '{templateName}' in assembly '{baseName}'. " +
                $"Available resources: {availableResources}");
        }

        // Load the template content
        using var stream = assembly.GetManifestResourceStream(manifestTemplateName);
        if (stream == null)
        {
            throw new FileNotFoundException(
                $"Template '{relativePath}' not found in embedded resources. " +
                $"Manifest resource name: '{manifestTemplateName}'");
        }

        using var reader = new StreamReader(stream);
        var templateContent = reader.ReadToEnd();

        // Parse and validate the template
        var template = Template.Parse(templateContent);
        if (template.HasErrors)
        {
            var errors = string.Join("; ", template.Messages.Select(m => m.ToString()));
            throw new InvalidOperationException(
                $"Template parsing errors in '{templateName}': {errors}");
        }

        return template;
    }
}