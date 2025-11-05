# Performance

!!! info "Coming Soon"
    This documentation is under development. Check back soon for performance benchmarks and optimization guidance.

## Overview

This page will cover:

- Build-time performance characteristics
- Runtime performance compared to reflection-based decorators
- Memory footprint analysis
- Benchmarks and comparisons
- Performance optimization tips

## Planned Content

- [ ] Build-time generation performance metrics
- [ ] Runtime overhead analysis (spoiler: zero!)
- [ ] Comparison with reflection-based decorator libraries
- [ ] Comparison with manual factory registration
- [ ] Memory allocation analysis
- [ ] Performance best practices
- [ ] Benchmarking methodology

## Key Performance Features

DecoWeaver is designed for zero runtime overhead:

- **No reflection** - All decorator wiring happens at compile time
- **No assembly scanning** - Interceptors redirect specific call sites only
- **No runtime registration** - DI container configured at build time via generated code
- **Incremental generation** - Only regenerates when source changes

## See Also

- [How It Works](../core-concepts/how-it-works.md) - Understanding compile-time generation
- [Requirements](../getting-started/requirements.md) - Runtime and SDK requirements
