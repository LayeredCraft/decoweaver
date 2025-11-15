# Changelog

All notable changes to DecoWeaver will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- No changes yet

## [1.0.5-beta] - 2025-11-14

### Changed
- **Migrated code generation from StringBuilder to Scriban templates** for improved maintainability and readability
  - Unified template generates all interceptor code for 8 registration kinds
  - Conditional logic handles variations (keyed, factory, instance, two-type-params)
  - Template caching with `ConcurrentDictionary<string, Template>` for performance
  - Strongly-typed `RegistrationModel` (readonly record struct) for zero-boxing overhead
- **Code cleanup** - Removed 570+ lines of StringBuilder emission code
  - Simplified `InterceptorEmitter.cs` from 440+ lines to ~70 lines
  - Removed individual template files (Common/InterceptsLocationAttribute, DecoratorKeys, DecoratorFactory)
  - Single unified template: `DecoWeaverInterceptors.scriban`
- **Comment reduction** - Removed generic "Register X" comments, kept only valuable WHY comments
  - "Create nested key to avoid circular resolution" - explains nested key purpose
  - "Compose decorators (innermost to outermost)" - explains decorator ordering

### Added
- Added dependency: `Scriban 6.5.0` and `Microsoft.CSharp 4.7.0` (for Scriban's dynamic features)
- Template resource format: `Templates.{FileName}.scriban` embedded in assembly
- `TemplateHelper.cs` with template loading and caching infrastructure
- `DecoWeaverInterceptorsSources.cs` with strongly-typed model generation

### Technical Details
- Template compilation is one-time cost with caching - no performance impact
- No functional changes - generated code is equivalent (except comment reduction)
- All 49 tests passing with updated snapshots
- Template uses Scriban's `{{-` syntax for whitespace control

## [1.0.4-beta] - 2025-11-13

### Added
- **Instance registration support** - Decorators now work with singleton instance registrations
  - `AddSingleton<TService>(instance)` - Single type parameter with instance
  - Decorators are applied around the provided instance
  - Only `AddSingleton` is supported (instance registrations don't exist for Scoped/Transient in .NET DI)

### Changed
- Extended `RegistrationKind` enum with `InstanceSingleTypeParam` variant
- Added `InstanceParameterName` field to `ClosedGenericRegistration` model
- Updated `ClosedGenericRegistrationProvider` to detect instance registrations (non-delegate second parameter)
- Updated `InterceptorEmitter` to generate instance interceptors with factory lambda wrapping
- Instance type is extracted from the actual argument expression (e.g., `new SqlRepository<Customer>()`)
- Instances are registered as keyed services via factory lambda (keyed services don't have instance overloads)

### Technical Details
- Instance detection: parameter type must match type parameter and NOT be a `Func<>` delegate
- Only `AddSingleton` accepted - `AddScoped`/`AddTransient` don't support instance parameters in .NET DI
- Instance wrapped in factory lambda: `services.AddKeyedSingleton<T>(key, (sp, _) => capturedInstance)`
- Type extraction uses `SemanticModel.GetTypeInfo(instanceArg).Type` to get actual implementation type
- Extension method ArgumentList doesn't include `this` parameter, so instance is at `args[0]`
- 3 new test cases (047-049) covering instance registration scenarios
- Updated sample project with instance registration example
- All existing functionality remains unchanged - this is purely additive

## [1.0.3-beta] - 2025-11-13

### Added
- **Keyed service support** - Decorators now work with keyed service registrations
  - `AddKeyedScoped<TService, TImplementation>(serviceKey)` - Keyed parameterless registration
  - `AddKeyedScoped<TService, TImplementation>(serviceKey, factory)` - Keyed with factory delegate (two-parameter)
  - `AddKeyedScoped<TService>(serviceKey, factory)` - Keyed with factory delegate (single-parameter)
  - All lifetimes supported: `AddKeyedScoped`, `AddKeyedTransient`, `AddKeyedSingleton`
  - Multiple keys per service type work independently
  - All key types supported: string, int, enum, custom objects
  - Nested key strategy prevents circular resolution while preserving user's original key

### Changed
- Extended `RegistrationKind` enum with three keyed service variants
- Added `ServiceKeyParameterName` field to `ClosedGenericRegistration` model
- Updated `ClosedGenericRegistrationProvider` to detect keyed service signatures (2 or 3 parameters)
- Updated `InterceptorEmitter` to generate keyed service interceptors with nested key strategy
- Added `ForKeyed` helper method to `DecoratorKeys` class for nested key generation

### Technical Details
- Added `RegistrationKind` values: `KeyedParameterless`, `KeyedFactoryTwoTypeParams`, `KeyedFactorySingleTypeParam`
- Keyed service detection validates `object?` type for service key parameter
- Factory delegates with keyed services detect `Func<IServiceProvider, object?, T>` signatures
- Nested key format: `"{userKey}|{ServiceAQN}|{ImplAQN}"` prevents conflicts between keys
- User's original key preserved for resolution via `GetRequiredKeyedService`
- 7 new test cases (039-045) covering keyed service scenarios
- Updated sample project with keyed service examples (string and integer keys, multiple keys)
- All existing functionality remains unchanged - this is purely additive

## [1.0.2-beta] - 2025-11-12

### Added
- **Factory delegate support** - Decorators now work with factory delegate registrations
  - `AddScoped<TService, TImplementation>(sp => new Implementation(...))` - Two-parameter generic overload
  - `AddScoped<TService>(sp => new Implementation(...))` - Single-parameter generic overload
  - All lifetimes supported: `AddScoped`, `AddTransient`, `AddSingleton`
  - Factory delegates can resolve dependencies from `IServiceProvider`
  - Decorator preservation - Factory logic is preserved while decorators are applied around the result

### Changed
- Extended `ClosedGenericRegistrationProvider` to detect and intercept factory delegate signatures
- Updated `InterceptorEmitter` to generate correct code for factory overloads
- Factory delegates are registered as keyed services, then wrapped with decorators
- Test case 022 renamed to `FactoryDelegate_SingleDecorator` to reflect new behavior

### Technical Details
- Added `RegistrationKind` enum (Parameterless, FactoryTwoTypeParams, FactorySingleTypeParam)
- Extended `ClosedGenericRegistration` model with optional `FactoryParameterName` field
- Factory transformers detect `Func<IServiceProvider, T>` signatures
- Generated interceptors preserve user's factory logic in keyed service registration
- 6 new test cases (033-038) covering factory delegate scenarios
- Updated sample project with factory delegate examples demonstrating complex dependencies
- All existing functionality remains unchanged - this is purely additive

## [1.0.1-beta] - 2025-11-10

### Added
- Assembly-level `[DecorateService(typeof(TService), typeof(TDecorator))]` attribute for applying decorators to all implementations of a service interface
- `[SkipAssemblyDecoration]` attribute for opting out of all assembly-level decorators
- `[DoNotDecorate(typeof(TDecorator))]` attribute for surgically excluding specific decorators from individual implementations
- Merge/precedence logic for combining class-level and assembly-level decorators
- Support for ordering assembly-level decorators via `Order` property
- Open generic matching in assembly-level decorators and DoNotDecorate directives
- 4 new test cases (029-032) covering assembly-level decorator scenarios

### Changed
- Decorator discovery pipeline now includes assembly-level attribute streams
- BuildDecorationMap now merges and deduplicates decorators from multiple sources
- Documentation restructured to include assembly-level decorator guides

### Technical Details
- Added `DecorateServiceAttribute` for assembly-level decoration
- Added `SkipAssemblyDecorationAttribute` for opting out of all assembly decorators
- Added `DoNotDecorateAttribute` for surgical decorator exclusion
- Added `ServiceDecoratedByProvider` for assembly attribute discovery
- Added `SkipAssemblyDecoratorProvider` for skip directive discovery
- Added `DoNotDecorateProvider` for opt-out directive discovery
- Filtering logic added to BuildDecorationMap for both skip and do-not-decorate support
- Comprehensive test coverage with snapshot verification

## [1.0.0-beta]

### Added
- Initial release of DecoWeaver
- Compile-time decorator registration using C# interceptors
- Support for `[DecoratedBy<T>]` generic attribute
- Support for `[DecoratedBy(typeof(T))]` non-generic attribute
- Multiple decorator support with ordering via `Order` property
- Closed generic support for specific instantiations like `IRepository<Customer>`
- Open generic decorators (e.g., `CachingRepository<>`) that are closed at runtime
- Incremental source generation for performance
- Zero runtime overhead with compile-time code generation
- Keyed services integration for circular dependency prevention
- Comprehensive documentation site with MkDocs Material
- Sample projects demonstrating usage patterns
- Complete test suite with 21 test cases and snapshot verification

### Technical Details
- Targets .NET 8.0+ runtime (for keyed services)
- Requires .NET SDK 8.0.400+ (for interceptor support)
- Requires C# 11+ language version
- Source generator built on Roslyn 4.14.0
- Attributes target netstandard2.0 for compatibility
- Uses XUnit v3 for testing with AutoFixture and NSubstitute

## Release Notes Format

Future releases will follow this format:

### [Version]

#### Added
- New features

#### Changed
- Changes to existing functionality

#### Deprecated
- Features that will be removed in future versions

#### Removed
- Features that have been removed

#### Fixed
- Bug fixes

#### Security
- Security vulnerability fixes

## Upgrade Guides

### From Beta to 1.0.0

When upgrading from beta to the production 1.0.0 release:

1. Update package reference to 1.0.0
2. Ensure .NET SDK 8.0.400+ is installed
3. Set `<LangVersion>11</LangVersion>` in project file
4. Rebuild solution to regenerate interceptors
5. Review changelog for any breaking changes

## Version Support

- **Latest**: Actively developed with new features and bug fixes
- **Previous Major**: Security fixes only for 6 months after new major release
- **Older Versions**: Not supported

## Breaking Changes Policy

DecoWeaver follows semantic versioning:

- **Major version** (x.0.0): Breaking changes, major features
- **Minor version** (1.x.0): New features, backwards compatible
- **Patch version** (1.0.x): Bug fixes, backwards compatible

Breaking changes will be clearly documented with:
- Migration guides
- Deprecation warnings in prior minor versions when possible
- Detailed explanation of changes

## Deprecation Policy

Features marked for deprecation:

1. Announced in minor release with `[Obsolete]` attribute
2. Supported for at least one more minor version
3. Removed in next major version

## Future Enhancements

Planned features for future releases:

### Under Consideration
- Decorator composition helpers
- Performance profiling decorators
- Additional diagnostic analyzers
- Integration with popular DI containers beyond Microsoft.Extensions.DependencyInjection

### Community Requests
- Additional open generic scenarios
- Enhanced IDE tooling support
- More example patterns

See the [GitHub Issues](https://github.com/layeredcraft/decoweaver/issues) page for active discussions.

## How to Stay Updated

- **GitHub Releases**: Watch the repository for release notifications
- **NuGet**: Subscribe to package updates
- **Documentation**: Check the docs site for latest changes

## Reporting Issues

Found a bug or have a feature request? Please:

1. Check existing [GitHub Issues](https://github.com/layeredcraft/decoweaver/issues)
2. Create a new issue with detailed description
3. Include code samples and environment details

See [Contributing Guide](contributing.md) for more details.