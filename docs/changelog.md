# Changelog

All notable changes to DecoWeaver will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- No changes yet

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
- Assembly-level `[DecorateService]` attribute (See [Issue #2](https://github.com/layeredcraft/decoweaver/issues/2))
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