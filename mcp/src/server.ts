#!/usr/bin/env node

import { fileURLToPath } from "node:url";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { StreamableHTTPServerTransport } from "@modelcontextprotocol/sdk/server/streamableHttp.js";
import express from "express";
import cors from "cors";
import { loadConfig } from "./config.js";
import { PsecsClient } from "./client.js";
import { loadOAuthProxyConfig } from "./oauth.js";
import { JwtIssuer } from "./jwt-issuer.js";
import { OAuthStore } from "./oauth-store.js";
import { setupOAuthProxy } from "./oauth-proxy.js";
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
 * Start the MCP server in OAuth HTTP mode.
 * Acts as an OAuth proxy: presents its own authorization server to MCP clients,
 * delegates user authentication to Auth0, provisions API keys, and issues
 * self-signed JWTs that the /mcp endpoint validates.
 */
async function startHttpOAuth(port: number, host: string): Promise<void> {
  const config = loadOAuthProxyConfig();
  const store = OAuthStore.createInMemory();
  const issuer = await JwtIssuer.create();

  const app = express();
  app.use(cors());
  app.use(express.json());
  app.use(express.urlencoded({ extended: true }));

  // Request logging — every inbound request
  app.use((req, _res, next) => {
    const auth = req.headers.authorization ? `Bearer ${req.headers.authorization.slice(7, 20)}...` : "none";
    console.error(`[psecs-mcp] ${req.method} ${req.path} auth=${auth} ip=${req.ip}`);
    next();
  });

  setupOAuthProxy(app, config, store, issuer);

  // Authenticated /mcp endpoint — validates self-issued JWTs,
  // looks up the cached API key, and handles the MCP request.
  // Support both /mcp and /v1/mcp (alias for cache-busting stale tokens)
  app.head(["/mcp", "/v1/mcp"], (_req, res) => {
    res.status(200).end();
  });

  app.all(["/mcp", "/v1/mcp"], async (req, res) => {
    const authHeader = req.headers.authorization;
    const token =
      authHeader && authHeader.toLowerCase().startsWith("bearer ")
        ? authHeader.slice(7)
        : undefined;

    if (!token) {
      res.setHeader(
        "WWW-Authenticate",
        `Bearer resource_metadata="${config.mcpBaseUrl}/.well-known/oauth-protected-resource", scope="psecs:play"`
      );
      res.status(401).json({ error: "Authentication required" });
      return;
    }

    try {
      const { jwtVerify, createLocalJWKSet } = await import("jose");
      const jwks = issuer.getJwks();
      const keySet = createLocalJWKSet(jwks);
      const { payload } = await jwtVerify(token, keySet, {
        issuer: config.mcpBaseUrl,
        audience: config.mcpBaseUrl,
        algorithms: ["RS256"],
      });

      if (!payload.sub) {
        res.status(401).json({ error: "Token missing sub claim" });
        return;
      }

      const apiKey = await store.getApiKey(payload.sub);
      if (!apiKey) {
        res.status(401).json({ error: "No API key found for user" });
        return;
      }

      const client = new PsecsClient({
        apiKey,
        baseUrl: config.psecsBaseUrl,
      });
      await handleMcpRequest(client, req, res);
    } catch (err) {
      console.error(
        "[psecs-mcp] Token validation failed:",
        err instanceof Error ? err.message : err
      );
      res.setHeader(
        "WWW-Authenticate",
        'Bearer error="invalid_token", error_description="Token validation failed"'
      );
      res.status(401).json({ error: "Token validation failed" });
    }
  });

  app.get("/health", (_req, res) => {
    res.json({ status: "ok", version: SERVER_VERSION, mode: "oauth" });
  });

  app.listen(port, host, () => {
    console.error(
      `[psecs-mcp] OAuth HTTP server listening on ${host}:${port}`
    );
    console.error(`[psecs-mcp] MCP Base URL: ${config.mcpBaseUrl}`);
    console.error(`[psecs-mcp] Auth0 domain: ${config.auth0Domain}`);
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
    await startHttpOAuth(portArg, host);
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
