# @psecs/mcp

MCP (Model Context Protocol) server for AI agents to play **PSECS** -- a multiplayer space commerce game where you manage corporations, explore star systems, mine resources, research technology, manufacture goods, and trade on the Nexus Market. PSECS is CLI/API-first and designed for automation; this MCP server gives AI agents native access to the full game.

## Installation

```bash
# Global install
npm install -g @psecs/mcp

# Or run directly
npx @psecs/mcp
```

## Configuration

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `PSECS_API_KEY` | Yes | -- | Your PSECS API key |
| `PSECS_BASE_URL` | No | `https://api.psecs.io` | API base URL |
| `PSECS_WEB_URL` | No | `https://psecs.io` | Web UI base URL (for wiki links in error messages) |

Get an API key from your PSECS account settings page.

## Usage with Claude Desktop

Add to your Claude Desktop configuration (`claude_desktop_config.json`):

```json
{
  "mcpServers": {
    "psecs": {
      "command": "npx",
      "args": ["-y", "@psecs/mcp"],
      "env": {
        "PSECS_API_KEY": "your-api-key-here"
      }
    }
  }
}
```

## Usage with Claude Code

```bash
claude mcp add psecs -- npx -y @psecs/mcp
```

## HTTP Mode

Run as an HTTP server instead of stdio for multi-client or remote setups:

```bash
npx @psecs/mcp --http --port 3000
```

The MCP endpoint is available at `POST /mcp` and a health check at `GET /health`.

## Available Tools

### Curated Gameplay Tools (25)

High-level tools that combine multiple API calls and include strategy hints in responses:

| Category | Tools |
|----------|-------|
| **Account** | `psecs_account_overview`, `psecs_create_corp` |
| **Fleet** | `psecs_fleet_status`, `psecs_explore_sector`, `psecs_navigate`, `psecs_scout_route`, `psecs_assess_threats` |
| **Extraction** | `psecs_mine_resource`, `psecs_extraction_status`, `psecs_optimize_extraction` |
| **Research** | `psecs_research_overview`, `psecs_allocate_research`, `psecs_tech_tree_path` |
| **Manufacturing** | `psecs_manufacturing_overview`, `psecs_start_manufacturing`, `psecs_manufacturing_status` |
| **Market** | `psecs_market_search`, `psecs_market_sell`, `psecs_market_buy_or_bid`, `psecs_market_portfolio` |
| **Ship** | `psecs_ship_manage_modules`, `psecs_ship_cargo_overview` |
| **Combat** | `psecs_engage_combat`, `psecs_combat_status`, `psecs_combat_summary` |

### Raw API Tools (98)

Auto-generated one-to-one mappings for every API endpoint, prefixed with `psecs_raw_`. Use these when the curated tools don't cover your specific need.

## Available Resources

### Static Guides (3)

- `psecs://guide/game-mechanics` -- Core game mechanics reference
- `psecs://guide/tech-tree-overview` -- Tech tree structure, tiers, and disciplines
- `psecs://guide/getting-started` -- New player walkthrough

### Dynamic Game State (8)

- `psecs://state/account` -- Profile, corp, credits, and strategy hints
- `psecs://state/fleets` -- Fleet positions, statuses, and navigation hints
- `psecs://state/research` -- Research allocations, progress, and optimization hints
- `psecs://state/manufacturing` -- Manufacturing queue, capacity, and production hints
- `psecs://state/market` -- Active listings, bids, and trading hints
- `psecs://state/inventory` -- Corp-wide resource totals
- `psecs://fleet/{fleetId}/status` -- Detailed status of a specific fleet
- `psecs://ship/{shipId}/status` -- Detailed status of a specific ship

## Prompts

- **`psecs_agent`** -- System prompt template for an AI agent playing PSECS. Includes game mechanics overview, tool guidance, and strategy fundamentals. Accepts an optional `playstyle` argument: `balanced`, `explorer`, `trader`, `industrialist`, or `aggressive`.

## License

MIT

