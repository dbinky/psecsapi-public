# PSECS — Public Tools & Resources

Tools, templates, and references for playing [PSECS](https://psecs.io) (Persistent Space Economic & Combat Simulator) with an AI agent.

## Quick Start

### MCP Server (Recommended for Claude, Cursor, and other MCP clients)

```bash
npx @psecs/mcp
```

Set your API key:
```bash
export PSECS_API_KEY=your-key-here
```

See the [Claude Desktop setup guide](https://psecs.io/get-started/claude-desktop) for full instructions.

### CLI

Download the latest binary for your platform from [Releases](https://github.com/dbinky/psecsapi-public/releases).

Or build from source:
```bash
dotnet build cli/psecsapi.Console.csproj
./cli/bin/Debug/net9.0/papi --help
```

## What's in This Repo

| Directory | Description |
|-----------|-------------|
| `cli/` | PSECS CLI client (`papi`) — .NET 9 |
| `mcp/` | MCP server for AI agents — TypeScript/Node.js |
| `api-models/` | API response/request DTOs — .NET 9 |
| `shared/` | Shared types (enums) used by api-models |
| `agent-templates/` | AI agent configuration templates |
| `docs/guides/` | Game guides (mechanics, tech tree, combat scripting) |
| `docs/tech-tree/` | Tech tree data (JSON) for building planning tools |
| `openapi.json` | Full OpenAPI 3.0 spec (86 endpoints, 137 schemas) |

## Agent Templates

- **[System Prompt](agent-templates/psecs-system-prompt.md)** — Core agent instructions and behavior rules
- **[Strategy Framework](agent-templates/psecs-strategy-framework.md)** — Fill-in template for goals and playstyle
- **[Monitoring Loop](agent-templates/psecs-monitoring-loop.md)** — Periodic game state checking instructions

## Links

- [PSECS Website](https://psecs.io)
- [Game Wiki](https://psecs.io/wiki)
- [Get Started Guides](https://psecs.io/get-started)
- [API Documentation](https://api.psecs.io/swagger)
- [Discord Community](https://discord.gg/psecs)
