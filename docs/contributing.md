# Contributing to Sculptor

Thank you for your interest in contributing to Sculptor! This guide will help you get started.

## Code of Conduct

By participating in this project, you agree to maintain a respectful and inclusive environment for all contributors.

## How Can I Contribute?

### Reporting Bugs

Before creating a bug report, please check the [existing issues](https://github.com/LayeredCraft/sculptor/issues) to avoid duplicates.

When reporting a bug, include:

- **Clear title and description**
- **Steps to reproduce** the issue
- **Expected behavior** vs actual behavior
- **Code samples** demonstrating the issue
- **Environment details** (OS, .NET SDK version, IDE)
- **Stack traces or error messages** if applicable

### Suggesting Features

Feature suggestions are welcome! Please:

- Check existing issues and discussions first
- Clearly describe the use case and benefits
- Provide examples of how the feature would be used
- Consider implementation complexity and maintainability

### Contributing Code

We welcome pull requests for:

- Bug fixes
- New features (discuss in an issue first)
- Documentation improvements
- Test coverage improvements
- Performance optimizations

## Development Setup

### Prerequisites

- **.NET SDK 8.0.400+**
- **Git**
- **IDE**: Visual Studio 2022 17.11+, Rider 2022.3+, or VS Code with C# Dev Kit

### Getting Started

1. **Fork the repository**

   ```bash
   # Fork via GitHub UI, then clone your fork
   git clone https://github.com/YOUR_USERNAME/sculptor.git
   cd sculptor
   ```

2. **Create a feature branch**

   ```bash
   git checkout -b feature/my-feature
   # or
   git checkout -b fix/my-bug-fix
   ```

3. **Restore dependencies**

   ```bash
   dotnet restore
   ```

4. **Build the solution**

   ```bash
   dotnet build
   ```

5. **Run tests**

   ```bash
   dotnet test
   ```

## Project Structure

```
sculptor/
├── src/
│   ├── LayeredCraft.Sculptor.Attributes/     # Runtime attributes
│   └── LayeredCraft.Sculptor.Generators/     # Source generator
├── samples/
│   └── Sculptor.Sample/                      # Sample project
├── test/
│   └── LayeredCraft.Sculptor.Generator.Tests/ # Unit tests
└── docs/                                      # Documentation
```

### Key Components

**Attributes Project**:
- Defines `[DecoratedBy<T>]` attributes
- Targets netstandard2.0 for broad compatibility
- Uses Polyfill for modern C# features

**Generators Project**:
- Incremental source generator implementation
- Uses Roslyn APIs for code analysis
- Emits interceptor code at compile time

**Tests Project**:
- XUnit v3 test framework
- Snapshot testing with Verify
- AutoFixture for test data generation

## Coding Guidelines

### C# Style

Follow standard C# conventions:

- Use PascalCase for public members
- Use camelCase for private fields with `_` prefix
- Use meaningful, descriptive names
- Keep methods focused and concise
- Add XML documentation comments for public APIs

```csharp
/// <summary>
/// Applies decorators to the specified implementation type.
/// </summary>
/// <param name="implementationType">The implementation type to decorate.</param>
/// <returns>A collection of decorator types to apply.</returns>
public IEnumerable<DecoratorInfo> GetDecorators(INamedTypeSymbol implementationType)
{
    // Implementation
}
```

### Source Generator Best Practices

- Use **incremental generation** for performance
- Keep **predicates fast** - syntax-only checks
- Use **equatable types** for change detection
- **Report clear diagnostics** for user errors
- **Test thoroughly** with various scenarios

### Testing

All code changes should include tests:

```csharp
[Theory]
[GeneratorAutoData]
public async Task Generator_AppliesDecorators_ForClassWithAttribute(
    SculptorGenerator generator,
    string[] casePaths)
{
    // Arrange
    var sources = await GeneratorTestHelpers.RunFromCases(generator, casePaths);

    // Act & Assert
    await VerifyGlue.VerifySourcesAsync(sources);
}
```

### Documentation

- Update documentation for new features
- Include code examples
- Explain the "why" not just the "what"
- Update changelog for user-facing changes

## Pull Request Process

### Before Submitting

1. **Ensure all tests pass**

   ```bash
   dotnet test
   ```

2. **Build successfully**

   ```bash
   dotnet build -c Release
   ```

3. **Update documentation** if needed

4. **Add tests** for new functionality

5. **Follow commit message conventions**

### Commit Messages

Use clear, descriptive commit messages:

```
feat: add support for assembly-level decorators
fix: resolve circular dependency with keyed services
docs: update quick start guide with new examples
test: add test cases for open generic decorators
refactor: simplify decorator resolution logic
```

**Format**: `type: description`

**Types**:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `test`: Test additions or changes
- `refactor`: Code refactoring
- `perf`: Performance improvements
- `chore`: Maintenance tasks

### Submitting the Pull Request

1. **Push your branch**

   ```bash
   git push origin feature/my-feature
   ```

2. **Create a pull request** on GitHub

3. **Fill out the PR template** with:
   - Description of changes
   - Related issue number (if applicable)
   - Testing performed
   - Breaking changes (if any)

4. **Address review feedback** promptly

5. **Ensure CI passes**

### PR Review Process

- Maintainers will review your PR
- Be responsive to feedback
- Make requested changes in new commits
- Once approved, maintainers will merge

## Testing

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test
dotnet test --filter "FullyQualifiedName~SculptorGeneratorTests.Generator_AppliesDecorators"

# Run with verbose output
dotnet test --logger "console;verbosity=detailed"
```

### Adding Test Cases

Test cases live in `/test/Cases/{NNN}_{Description}/`:

1. Create directory for your test case
2. Add source files demonstrating the scenario
3. Create test method using `[GeneratorAutoData]`
4. Call `VerifyGlue.VerifySourcesAsync()`

```csharp
[Theory]
[GeneratorAutoData]
public async Task Generator_HandlesMultipleDecorators(
    SculptorGenerator generator,
    string[] casePaths)
{
    var sources = await GeneratorTestHelpers.RunFromCases(generator, casePaths);
    await VerifyGlue.VerifySourcesAsync(sources);
}
```

## Documentation

Documentation is built with MkDocs Material:

### Local Preview

```bash
# Install dependencies
pip install -r requirements.txt

# Serve locally
mkdocs serve

# Open http://localhost:8000
```

### Building Docs

```bash
mkdocs build
```

### Documentation Structure

- **Getting Started**: Installation, requirements, quick start
- **Core Concepts**: How it works, decorator pattern, ordering
- **Usage**: Class-level decorators, multiple decorators, open generics
- **Examples**: Real-world patterns (caching, logging, etc.)
- **Advanced**: Interceptors, source generators, testing
- **API Reference**: Detailed attribute documentation

## Release Process

Maintainers handle releases:

1. Update version in project files
2. Update CHANGELOG.md
3. Create Git tag
4. Push to GitHub
5. CI builds and publishes to NuGet

## Getting Help

- **GitHub Issues**: Bug reports and feature requests
- **GitHub Discussions**: Questions and community support
- **Documentation**: https://layeredcraft.github.io/sculptor/

## Recognition

Contributors are recognized in:
- GitHub contributors page
- Release notes for significant contributions

Thank you for contributing to Sculptor!