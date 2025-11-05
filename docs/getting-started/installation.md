# Installation

## NuGet Package

Install DecoWeaver via the .NET CLI:

```bash
dotnet add package DecoWeaver --prerelease
```

Or via Package Manager Console in Visual Studio:

```powershell
Install-Package DecoWeaver -Prerelease
```

Or add directly to your `.csproj` file:

```xml
<PackageReference Include="DecoWeaver" Version="1.0.0-beta.*" />
```

Note: The full version includes a build number suffix added by the build system (e.g., `1.0.0-beta.123`).

## Package Contents

The DecoWeaver NuGet package includes:

- **DecoWeaver.Attributes.dll** - Runtime attributes (zero footprint with `[Conditional]`)
- **DecoWeaver.dll** - Source generator (build-time only)

## Verify Installation

After installation, verify the generator is working:

1. Add a simple decorator attribute to a class
2. Build your project
3. Check for generated files in `obj/Debug/{targetFramework}/generated/DecoWeaver/`

## IDE Support

### Visual Studio 2022

- Requires **Visual Studio 2022 17.4+** for C# 11 support
- Enable **"Show generated files"** in Solution Explorer options to view generated code

### JetBrains Rider

- Requires **Rider 2022.3+** for C# 11 support
- Generated files appear in the **Generated Files** node in Solution Explorer

### Visual Studio Code

- Requires **C# Dev Kit** extension
- Ensure you're targeting .NET 8+ with C# 11 in your project file

## Project Configuration

Ensure your project targets C# 11 or later:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>11</LangVersion> <!-- Or "latest" -->
  </PropertyGroup>
</Project>
```

## Next Steps

- Review [Requirements](requirements.md) for detailed prerequisites
- Follow the [Quick Start](quick-start.md) guide to create your first decorator