#!/usr/bin/env node

import { fileURLToPath } from "node:url";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { StreamableHTTPServerTransport } from "@modelcontextprotocol/sdk/server/streamableHttp.js";
import express from "express";
import { loadConfig } from "./config.js";
import { PsecsClient } from "./client.js";
import {
  loadOAuthConfig,
  validateAccessToken,
  provisionApiKey,
  type OAuthConfig,
} from "./oauth.js";
import { registerRawTools } from "./generated/raw-tools.js";
import { registerAccountTools } from "./tools/account.js";
import { registerFleetTools } from "./tools/fleet.js";
import { registerExtractionTools } from "./tools/extraction.js";
import { registerResearchTools } from "./tools/research.js";
import { registerManufacturingTools } from "./tools/manufacturing.js";
import { registerMarketTools } from "./tools/market.js";
import { registerShipTools } from "./tools/ship.js";
import { registerCombatTools } from "./tools/combat.js";
import { registerShipyardTools } from "./tools/shipyard.js";
import { registerLootTools } from "./tools/loot.js";
import { registerCargoTools } from "./tools/cargo.js";
import { registerCatalogTools } from "./tools/catalog.js";
import { registerWarehouseTools } from "./tools/warehouse.js";
import { registerTokenTools } from "./tools/tokens.js";
import { registerMintTools } from "./tools/mint.js";
import { registerCliTools } from "./tools/cli.js";
import { registerGuideResources } from "./resources/guides.js";
import { registerGameStateResources } from "./resources/game-state.js";
import { registerPrompts } from "./prompts/game-guide.js";

const SERVER_NAME = "psecs";
const SERVER_VERSION = "0.0.1";

/**
 * Handle an incoming MCP request: create a per-request server+transport,
 * process the request, and tear down on response close.
 */
async function handleMcpRequest(
  client: PsecsClient,
  req: express.Request,
  res: express.Response
): Promise<void> {
  const server = createServer(client);
  const transport = new StreamableHTTPServerTransport({
    sessionIdGenerator: undefined,
  });

  await server.connect(transport);
  await transport.handleRequest(req, res, req.body);

  res.on("close", () => {
    transport.close().catch(() => {});
    server.close().catch(() => {});
  });
}

/**
 * Create and configure an McpServer with all registered tools.
 */
export function createServer(client: PsecsClient): McpServer {
  const server = new McpServer({
    name: SERVER_NAME,
    version: SERVER_VERSION,
  });
  // Raw API tools (auto-generated)
  registerRawTools(server, client);

  // Curated gameplay tools
  registerAccountTools(server, client);
  registerFleetTools(server, client);
  registerExtractionTools(server, client);
  registerResearchTools(server, client);
  registerManufacturingTools(server, client);
  registerMarketTools(server, client);
  registerShipTools(server, client);
  registerCombatTools(server, client);
  registerShipyardTools(server, client);
  registerLootTools(server, client);
  registerCargoTools(server, client);
  registerCatalogTools(server, client);
  registerWarehouseTools(server, client);
  registerTokenTools(server, client);
  registerMintTools(server, client);
  registerCliTools(server);

  // Resources
  registerGuideResources(server, client);
  registerGameStateResources(server, client);

  // Prompts
  registerPrompts(server, client);

  return server;
}

/**
 * Start the MCP server in stdio mode.
 * Communicates over stdin/stdout using JSON-RPC.
 * All diagnostic output goes to stderr since stdout is the transport.
 */
async function startStdio(client: PsecsClient): Promise<void> {
  const server = createServer(client);
  const transport = new StdioServerTransport();
  await server.connect(transport);
  console.error(`[psecs-mcp] stdio server running (v${SERVER_VERSION})`);
}

/**
 * Start the MCP server in HTTP mode using Express.
 * Uses StreamableHTTPServerTransport in stateless mode — a fresh
 * server+transport pair is created per request for clean isolation.
 */
async function startHttp(
  client: PsecsClient,
  port: number,
  host: string
): Promise<void> {
  const app = express();
  app.use(express.json());

  // MCP endpoint — handles POST, GET, DELETE for the Streamable HTTP protocol
  app.all("/mcp", async (req, res) => {
    try {
      await handleMcpRequest(client, req, res);
    } catch (err) {
      console.error("[psecs-mcp] Error handling /mcp request:", err);
      if (!res.headersSent) {
        res.status(500).json({ error: "Internal server error" });
      }
    }
  });

  // Health check endpoint
  app.get("/health", (_req, res) => {
    res.json({ status: "ok", version: SERVER_VERSION });
  });

  app.listen(port, host, () => {
    console.error(
      `[psecs-mcp] HTTP server listening on ${host}:${port}`
    );
  });
}

/**
 * Register all OAuth routes on the given Express app.
 * Exported for testing — does not bind a port.
 */
export function setupOAuthRoutes(
  app: ReturnType<typeof express>,
  oauthConfig: OAuthConfig,
  apiKeyCache: Map<string, string>
): void {
  app.get("/.well-known/oauth-protected-resource", (_req, res) => {
    res.json({
      resource: oauthConfig.auth0Audience,
      authorization_servers: [oauthConfig.issuerUrl],
      scopes_supported: ["psecs:play"],
      bearer_methods_supported: ["header"],
    });
  });

  app.head("/mcp", (_req, res) => {
    res.status(200).end();
  });

  app.all("/mcp", async (req, res) => {
    const authHeader = req.headers.authorization;
    // RFC 6750 / RFC 9110: auth-scheme is case-insensitive; "bearer" and "BEARER" are valid.
    const token =
      authHeader && authHeader.toLowerCase().startsWith("bearer ")
        ? authHeader.slice(7)
        : undefined;

    if (!token) {
      res.setHeader(
        "WWW-Authenticate",
        `Bearer resource_metadata="${oauthConfig.auth0Audience}/.well-known/oauth-protected-resource", scope="psecs:play"`
      );
      res.status(401).json({ error: "Authentication required" });
      return;
    }

    const tokenResult = await validateAccessToken(token, oauthConfig);
    if (!tokenResult.ok) {
      console.error(`[psecs-mcp] Token validation failed: ${tokenResult.error}`);
      res.setHeader(
        "WWW-Authenticate",
        `Bearer error="invalid_token", error_description="Token validation failed"`
      );
      res.status(401).json({ error: "Token validation failed" });
      return;
    }

    let apiKey = apiKeyCache.get(tokenResult.userId);
    if (!apiKey) {
      const provisionResult = await provisionApiKey(
        tokenResult.userId,
        oauthConfig
      );
      if (!provisionResult.ok) {
        console.error(
          `[psecs-mcp] API key provisioning failed for ${tokenResult.userId}: ${provisionResult.error}`
        );
        res.status(502).json({
          error: "Failed to provision API access. Please try again.",
        });
        return;
      }
      apiKey = provisionResult.apiKey;
      apiKeyCache.set(tokenResult.userId, apiKey);
    }

    try {
      const client = new PsecsClient({
        apiKey,
        baseUrl: oauthConfig.psecsBaseUrl,
      });
      await handleMcpRequest(client, req, res);
    } catch (err) {
      console.error("[psecs-mcp] Error handling /mcp request:", err);
      if (!res.headersSent) {
        res.status(500).json({ error: "Internal server error" });
      }
    }
  });

  app.get("/health", (_req, res) => {
    res.json({ status: "ok", version: SERVER_VERSION, mode: "oauth" });
  });
}

/**
 * Start the MCP server in OAuth HTTP mode.
 * Validates Auth0 JWTs, provisions API keys per user, and creates
 * per-request PsecsClient instances with the provisioned key.
 */
async function startHttpOAuth(
  oauthConfig: OAuthConfig,
  port: number,
  host: string
): Promise<void> {
  const app = express();
  app.use(express.json());

  setupOAuthRoutes(app, oauthConfig, new Map<string, string>());

  app.listen(port, host, () => {
    console.error(
      `[psecs-mcp] OAuth HTTP server listening on ${host}:${port}`
    );
    console.error(`[psecs-mcp] Auth0 issuer: ${oauthConfig.issuerUrl}`);
    console.error(`[psecs-mcp] Audience: ${oauthConfig.auth0Audience}`);
  });
}

/**
 * Parse CLI args and start the server in the appropriate mode.
 */
async function main(): Promise<void> {
  const args = process.argv.slice(2);
  const httpFlag = args.includes("--http");
  const oauthFlag = args.includes("--oauth");

  const portIndex = args.indexOf("--port");
  const portArg = portIndex !== -1 ? parseInt(args[portIndex + 1], 10) : 3001;
  if (isNaN(portArg) || portArg < 1 || portArg > 65535) {
    throw new Error(
      `Invalid port number: "${args[portIndex + 1]}". Must be an integer between 1 and 65535.`
    );
  }

  const hostIndex = args.indexOf("--host");
  const host = hostIndex !== -1 ? args[hostIndex + 1] : "127.0.0.1";

  if (oauthFlag) {
    if (!httpFlag) {
      throw new Error("--oauth requires --http (OAuth mode only works with HTTP transport)");
    }
    const oauthConfig = loadOAuthConfig();
    await startHttpOAuth(oauthConfig, portArg, host);
  } else if (httpFlag) {
    const config = loadConfig();
    const client = new PsecsClient(config);
    await startHttp(client, portArg, host);
  } else {
    const config = loadConfig();
    const client = new PsecsClient(config);
    await startStdio(client);
  }
}

// Only run main() when this file is the process entry point, not when imported by tests.
if (process.argv[1] === fileURLToPath(import.meta.url)) {
  main().catch((err) => {
    console.error("[psecs-mcp] Fatal error:", err);
    process.exit(1);
  });
}
