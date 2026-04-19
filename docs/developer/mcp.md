# MCP Developer Tooling

XcaNet treats MCP as optional developer tooling only.

- MCP is not part of the shipped desktop application.
- MCP is not required for `dotnet build`, `dotnet test`, packaging, or runtime startup.
- End users do not need MCP to run XcaNet.

This repo documents two developer-oriented MCP integrations:

- Microsoft Learn MCP for trusted Microsoft documentation
- Avalonia Build MCP for Avalonia docs and UI guidance

## Recommended usage

Use Microsoft Learn MCP when working on:

- .NET APIs
- `Microsoft.Extensions.*`
- EF Core and general Microsoft platform guidance
- packaging or platform notes that depend on Microsoft documentation

Use Avalonia Build MCP when working on:

- Avalonia controls and APIs
- XAML patterns
- theme-safe styling guidance
- desktop UI implementation details

This repo intentionally does not treat DevTools MCP or Parcel MCP as active project tooling for normal development.

## Microsoft Learn MCP

Official overview:

- `https://learn.microsoft.com/en-us/training/support/mcp`
- Developer reference: `https://learn.microsoft.com/en-us/training/support/mcp-developer-reference`

Endpoint:

- `https://learn.microsoft.com/api/mcp`

Microsoft documents the Learn server as a remote Streamable HTTP MCP server. It is intended to be consumed through MCP-capable clients rather than browsed directly.

## Avalonia Build MCP

Official docs:

- `https://docs.avaloniaui.net/tools/ai-tools/build-mcp`

Endpoint:

- `https://docs-mcp.avaloniaui.net/mcp`

Avalonia documents Build MCP as a remote MCP server for documentation search and API lookup. It is the preferred Avalonia MCP integration for this repo because it improves doc-grounded UI work without adding runtime dependencies.

## Example workspace configuration

An editor-agnostic example is checked in at:

- `tooling/mcp/workspace.mcp.example.json`

You can adapt it for workspace-level MCP configuration in tools such as VS Code, Visual Studio, Rider, Cursor, Claude Code, or other MCP-capable clients.

## Suggested team guidance

When using AI-assisted development in this repo:

- use Microsoft Learn MCP for narrow Microsoft/.NET reference questions
- use Avalonia Build MCP for Avalonia docs and UI implementation questions
- keep MCP optional in local workflows and CI
- do not add runtime dependencies on MCP

## What not to do

- Do not make MCP a startup requirement.
- Do not document MCP as an operator feature.
- Do not assume every contributor uses the same editor or MCP client.
- Do not bypass official docs when Microsoft Learn MCP or Avalonia Build MCP can answer the question directly.
