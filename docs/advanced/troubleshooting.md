# Troubleshooting

!!! info "Coming Soon"
    This documentation is under development. Check back soon for common issues and solutions.

## Overview

This page will provide solutions to common problems encountered when using DecoWeaver.

## Planned Content

- [ ] Generator not running
- [ ] Decorators not being applied
- [ ] Circular dependency errors
- [ ] Generic type resolution issues
- [ ] C# language version errors
- [ ] IDE integration issues
- [ ] Build warnings and how to resolve them
- [ ] Diagnostic error codes (DECOW###)

## Quick Diagnostics

While detailed troubleshooting content is being developed, here are some quick checks:

### Verify Installation
```bash
dotnet list package | grep DecoWeaver
```

### Check Generated Files
Look in `obj/Debug/{targetFramework}/generated/DecoWeaver/` for generated interceptor code.

### Verify C# Version
Ensure your project targets C# 11 or later:
```xml
<LangVersion>11</LangVersion>
```

### Check Build Output
Look for DecoWeaver diagnostics with code `DECOW###` in build output.

## Getting Help

If you encounter issues not covered here:

1. Check existing [GitHub Issues](https://github.com/LayeredCraft/decoweaver/issues)
2. Review the [Requirements](../getting-started/requirements.md) page
3. Open a new issue with reproduction steps

## See Also

- [Requirements](../getting-started/requirements.md) - Prerequisites and configuration
- [How It Works](../core-concepts/how-it-works.md) - Understanding the generation process
