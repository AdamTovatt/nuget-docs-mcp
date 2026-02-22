# NugetDocsMcp

[![Tests](https://github.com/AdamTovatt/nuget-docs-mcp/actions/workflows/dotnet.yml/badge.svg)](https://github.com/AdamTovatt/nuget-docs-mcp/actions/workflows/dotnet.yml)
[![NuGet Version](https://img.shields.io/nuget/v/NugetDocsMcp.svg)](https://www.nuget.org/packages/NugetDocsMcp)
[![NuGet Downloads](https://img.shields.io/nuget/dt/NugetDocsMcp.svg)](https://www.nuget.org/packages/NugetDocsMcp)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](https://opensource.org/licenses/MIT)

A tool for finding documentation for locally-installed NuGet packages. Works as both a CLI tool and an MCP (Model Context Protocol) server for AI agents.

## Installation

```bash
dotnet tool install --global NugetDocsMcp
```

After installation, the `nd` command will be available globally.

To update to the latest version:

```bash
dotnet tool update --global NugetDocsMcp
```

To uninstall:

```bash
dotnet tool uninstall --global NugetDocsMcp
```

To register it as an MCP tool in Claude Code:

```bash
claude mcp add nugetdocs -- nd --mcp
```

For Cursor or other MCP clients, add this to your MCP configuration:

```json
{
  "mcpServers": {
    "nugetdocs": {
      "command": "nd",
      "args": ["--mcp"]
    }
  }
}
```

## Usage

```bash
nd readme <package> [--version <v>]                          # Get the README for a package
nd types <package> [--version <v>] [--framework <f>]         # List all types in a package
nd search <package> <query> [--version <v>] [--framework <f>] # Search docs by member name
nd help                                                       # Show help information
```

### Examples

```bash
# Read a package's README
nd readme Dapper

# List all types in a package
nd types Microsoft.Extensions.DependencyInjection.Abstractions --version 10.0.1 --framework net8.0

# Search for a method by short name
nd search Dapper Query

# Search by qualified name
nd search Dapper "SqlMapper.Query" --version 2.1.35 --framework net8.0
```

## Behavior

### Version and framework auto-resolve

If only one version of a package is installed locally, it is used automatically. Same for target framework. If multiple exist and none is specified, the tool lists the available options so you can pick one.

### Reads from the local NuGet cache

All lookups read from the global NuGet packages folder (`~/.nuget/packages/`). Only packages that have already been restored are available.

### XML documentation

`nd search` and `nd types` read the XML documentation files that ship with NuGet packages. Not all packages include XML docs (e.g. Moq does not).

### Case-insensitive matching

Package names, version lookups, README detection, and search queries all use case-insensitive matching.

## As MCP Server

```bash
nd --mcp
```

When running as an MCP server, the following tools are available:

- `nd_readme(packageName, version?)` - Get the README for a package
- `nd_types(packageName, version?, targetFramework?)` - List all types in a package
- `nd_search(packageName, query, version?, targetFramework?)` - Search docs by member name
- `nd_help()` - Get help

## Development

```bash
git clone <repository-url>
cd nuget-docs-mcp
dotnet build NugetDocsMcp.slnx
dotnet test NugetDocsMcp.slnx
```

To run as MCP server during development:

```bash
dotnet run --project NugetDocsMcp.Cli/NugetDocsMcp.Cli.csproj -- --mcp
```

To package:

```bash
dotnet pack NugetDocsMcp.Cli/NugetDocsMcp.Cli.csproj --configuration Release
```

## License

MIT License
