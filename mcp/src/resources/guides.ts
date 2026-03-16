import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { PsecsClient } from "../client.js";
import { readFileSync } from "node:fs";
import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const GUIDES_DIR = resolve(__dirname, "../../content/guides");

function loadGuide(filename: string): string {
  return readFileSync(resolve(GUIDES_DIR, filename), "utf-8");
}

// Load all guides at module init (startup) — not per-request
const GUIDES = {
  "game-mechanics": loadGuide("game-mechanics.md"),
  "tech-tree-overview": loadGuide("tech-tree-overview.md"),
  "combat-scripting": loadGuide("combat-scripting.md"),
  "combat-simulation": loadGuide("combat-simulation.md"),
  "getting-started": loadGuide("getting-started.md"),
  "cli-setup": loadGuide("cli-setup.md"),
};

export function registerGuideResources(
  server: McpServer,
  _client: PsecsClient
): void {
  server.registerResource(
    "game-mechanics",
    "psecs://guide/game-mechanics",
    {
      description: "Core game mechanics reference for AI agents",
    },
    async (uri) => ({
      contents: [
        {
          uri: uri.href,
          mimeType: "text/markdown",
          text: GUIDES["game-mechanics"],
        },
      ],
    })
  );

  server.registerResource(
    "tech-tree-overview",
    "psecs://guide/tech-tree-overview",
    {
      description: "Tech tree structure, tiers, and disciplines",
    },
    async (uri) => ({
      contents: [
        {
          uri: uri.href,
          mimeType: "text/markdown",
          text: GUIDES["tech-tree-overview"],
        },
      ],
    })
  );

  server.registerResource(
    "combat-scripting",
    "psecs://guide/combat-scripting",
    {
      description:
        "Combat scripting API reference — JavaScript commands, state object, utilities, and examples",
    },
    async (uri) => ({
      contents: [
        {
          uri: uri.href,
          mimeType: "text/markdown",
          text: GUIDES["combat-scripting"],
        },
      ],
    })
  );

  server.registerResource(
    "combat-simulation",
    "psecs://guide/combat-simulation",
    {
      description:
        "Combat simulation workflow — local testing, CLI commands, fleet export, terrain types, and autonomous script iteration loop",
    },
    async (uri) => ({
      contents: [
        {
          uri: uri.href,
          mimeType: "text/markdown",
          text: GUIDES["combat-simulation"],
        },
      ],
    })
  );

  server.registerResource(
    "getting-started",
    "psecs://guide/getting-started",
    {
      description:
        "New player guide — first steps after creating a corporation",
    },
    async (uri) => ({
      contents: [
        {
          uri: uri.href,
          mimeType: "text/markdown",
          text: GUIDES["getting-started"],
        },
      ],
    })
  );

  server.registerResource(
    "cli-setup",
    "psecs://guide/cli-setup",
    {
      description:
        "CLI download, installation, and setup guide for the papi command-line tool",
    },
    async (uri) => ({
      contents: [
        {
          uri: uri.href,
          mimeType: "text/markdown",
          text: GUIDES["cli-setup"],
        },
      ],
    })
  );
}
