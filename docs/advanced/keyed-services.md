# Keyed Services Strategy

!!! info "Coming Soon"
    This documentation is under development. Check back soon for detailed information about how DecoWeaver uses keyed services internally.

## Overview

This page will cover:

- How DecoWeaver uses .NET 8.0+ keyed services to prevent circular dependencies
- The internal keying strategy for undecorated implementations
- Why keyed services are necessary for decorator chains
- Performance implications

## Planned Content

- [ ] Keyed services architecture explanation
- [ ] Key format and naming conventions
- [ ] How the generator creates keyed registrations
- [ ] Resolving decorated vs undecorated instances
- [ ] Troubleshooting keyed service issues

## Requirements

DecoWeaver requires .NET 8.0+ runtime specifically because it leverages the keyed services feature introduced in that version for its internal implementation.

## See Also

- [How It Works](../core-concepts/how-it-works.md) - Understanding the generation process
- [Interceptors](interceptors.md) - How interceptors redirect DI calls
