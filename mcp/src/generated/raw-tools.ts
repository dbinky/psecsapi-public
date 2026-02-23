// Auto-generated from openapi.json — do not edit by hand
// Run: npm run generate

import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { PsecsClient } from "../client.js";
import { formatToolResult, formatToolError } from "../tool-utils.js";

export function registerRawTools(server: McpServer, client: PsecsClient): void {

  // GET /api/Auth/api-key/status
  server.registerTool(
    "psecs_raw_auth_api_key_status",
    {
      description: "Check if an API key exists (does not return the key).",
    },
    async () => {
      const result = await client.get("/api/Auth/api-key/status", undefined);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // POST /api/cargo/transfer
  server.registerTool(
    "psecs_raw_create_cargo_transfer",
    {
      description: "Transfer a cargo asset between two ships in the same fleet.",
      inputSchema: {
        fleetId: z.string().optional(),
        body: z.string().describe("Request body as JSON string"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.query = {
        fleetId: args.fleetId,
      };
      let parsedBody: unknown;
      try { parsedBody = JSON.parse(args.body); } catch { return { isError: true, content: [{ type: "text" as const, text: "Invalid JSON in request body" }] }; }
      const result = await client.post("/api/cargo/transfer", parsedBody, options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // POST /api/cargo/move
  server.registerTool(
    "psecs_raw_create_cargo_move",
    {
      description: "Move a cargo asset between holds on the same ship.",
      inputSchema: {
        shipId: z.string().optional(),
        body: z.string().describe("Request body as JSON string"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.query = {
        shipId: args.shipId,
      };
      let parsedBody: unknown;
      try { parsedBody = JSON.parse(args.body); } catch { return { isError: true, content: [{ type: "text" as const, text: "Invalid JSON in request body" }] }; }
      const result = await client.post("/api/cargo/move", parsedBody, options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/corp/{corpId}/Catalog
  server.registerTool(
    "psecs_raw_corp_catalog",
    {
      description: "Get all catalog entries for a corporation.",
      inputSchema: {
        corpId: z.string().describe("Corporation ID"),
        type: z.string().describe("Filter by resource type (Mineral, Chemical, Flora, Fauna, Microscopic)").optional(),
        resourceClass: z.string().describe("Filter by resource class (Metal, Ore, Gemstone, etc.)").optional(),
        favoritesOnly: z.coerce.boolean().describe("If true, only return favorited entries").optional(),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        corpId: String(args.corpId),
      };
      options.query = {
        type: args.type,
        resourceClass: args.resourceClass,
        favoritesOnly: args.favoritesOnly,
      };
      const result = await client.get("/api/corp/{corpId}/Catalog", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/corp/{corpId}/Catalog/{entryId}
  server.registerTool(
    "psecs_raw_corp_catalog_by_entry",
    {
      description: "Get a specific catalog entry.",
      inputSchema: {
        corpId: z.string().describe("Corporation ID"),
        entryId: z.string().describe("Catalog entry ID"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        corpId: String(args.corpId),
        entryId: String(args.entryId),
      };
      const result = await client.get("/api/corp/{corpId}/Catalog/{entryId}", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // POST /api/corp/{corpId}/Catalog/{entryId}/favorite
  server.registerTool(
    "psecs_raw_create_corp_catalog_favorite",
    {
      description: "Mark a catalog entry as a favorite.",
      inputSchema: {
        corpId: z.string().describe("Corporation ID"),
        entryId: z.string().describe("Catalog entry ID"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        corpId: String(args.corpId),
        entryId: String(args.entryId),
      };
      const result = await client.post("/api/corp/{corpId}/Catalog/{entryId}/favorite", undefined, options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // DELETE /api/corp/{corpId}/Catalog/{entryId}/favorite
  server.registerTool(
    "psecs_raw_delete_corp_catalog_favorite",
    {
      description: "Remove a catalog entry from favorites.",
      inputSchema: {
        corpId: z.string().describe("Corporation ID"),
        entryId: z.string().describe("Catalog entry ID"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        corpId: String(args.corpId),
        entryId: String(args.entryId),
      };
      const result = await client.delete("/api/corp/{corpId}/Catalog/{entryId}/favorite", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // PUT /api/corp/{corpId}/Catalog/{entryId}/note
  server.registerTool(
    "psecs_raw_update_corp_catalog_note",
    {
      description: "Set a personal note on a catalog entry.",
      inputSchema: {
        corpId: z.string().describe("Corporation ID"),
        entryId: z.string().describe("Catalog entry ID"),
        body: z.string().describe("Note content (max 500 characters) (JSON string)"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        corpId: String(args.corpId),
        entryId: String(args.entryId),
      };
      let parsedBody: unknown;
      try { parsedBody = JSON.parse(args.body); } catch { return { isError: true, content: [{ type: "text" as const, text: "Invalid JSON in request body" }] }; }
      const result = await client.put("/api/corp/{corpId}/Catalog/{entryId}/note", parsedBody, options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // DELETE /api/corp/{corpId}/Catalog/{entryId}/note
  server.registerTool(
    "psecs_raw_delete_corp_catalog_note",
    {
      description: "Clear the note from a catalog entry.",
      inputSchema: {
        corpId: z.string().describe("Corporation ID"),
        entryId: z.string().describe("Catalog entry ID"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        corpId: String(args.corpId),
        entryId: String(args.entryId),
      };
      const result = await client.delete("/api/corp/{corpId}/Catalog/{entryId}/note", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // POST /api/combat/engage
  server.registerTool(
    "psecs_raw_create_combat_engage",
    {
      description: "Initiate fleet-vs-fleet combat. The attacker fleet must belong to the authenticated user's corp.\r\nBoth fleets must be Idle and in the same non-Nexus sector.",
      inputSchema: {
        body: z.string().describe("Attacker and target fleet IDs (JSON string)"),
      },
    },
    async (args) => {
      let parsedBody: unknown;
      try { parsedBody = JSON.parse(args.body); } catch { return { isError: true, content: [{ type: "text" as const, text: "Invalid JSON in request body" }] }; }
      const result = await client.post("/api/combat/engage", parsedBody, undefined);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/combat/{combatId}/status
  server.registerTool(
    "psecs_raw_combat_status",
    {
      description: "Check the current status of a combat instance.",
      inputSchema: {
        combatId: z.string().describe("Combat instance ID returned from engage"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        combatId: String(args.combatId),
      };
      const result = await client.get("/api/combat/{combatId}/status", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/combat/{combatId}/summary
  server.registerTool(
    "psecs_raw_combat_summary",
    {
      description: "Get the full summary of a completed combat. Summary data persists indefinitely.",
      inputSchema: {
        combatId: z.string().describe("Combat instance ID"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        combatId: String(args.combatId),
      };
      const result = await client.get("/api/combat/{combatId}/summary", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/combat/{combatId}/replay
  server.registerTool(
    "psecs_raw_combat_replay",
    {
      description: "Download the Protobuf replay binary for client-side playback.\r\nReplay data is retained for 90 days after combat; after that only the summary remains.",
      inputSchema: {
        combatId: z.string().describe("Combat instance ID"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        combatId: String(args.combatId),
      };
      const result = await client.get("/api/combat/{combatId}/replay", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/corp/{corpId}/scripts
  server.registerTool(
    "psecs_raw_corp_scripts",
    {
      description: "List all combat scripts for a corporation (without source code).",
      inputSchema: {
        corpId: z.string().describe("Corporation ID"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        corpId: String(args.corpId),
      };
      const result = await client.get("/api/corp/{corpId}/scripts", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // POST /api/corp/{corpId}/scripts
  server.registerTool(
    "psecs_raw_create_corp_scripts",
    {
      description: "Create a new combat script.",
      inputSchema: {
        corpId: z.string().describe("Corporation ID"),
        body: z.string().describe("Script name and source code (JSON string)"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        corpId: String(args.corpId),
      };
      let parsedBody: unknown;
      try { parsedBody = JSON.parse(args.body); } catch { return { isError: true, content: [{ type: "text" as const, text: "Invalid JSON in request body" }] }; }
      const result = await client.post("/api/corp/{corpId}/scripts", parsedBody, options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/corp/{corpId}/scripts/{scriptId}
  server.registerTool(
    "psecs_raw_corp_scripts_by_script",
    {
      description: "Get a specific combat script including its source code.",
      inputSchema: {
        corpId: z.string().describe("Corporation ID"),
        scriptId: z.string().describe("Script ID"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        corpId: String(args.corpId),
        scriptId: String(args.scriptId),
      };
      const result = await client.get("/api/corp/{corpId}/scripts/{scriptId}", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // PUT /api/corp/{corpId}/scripts/{scriptId}
  server.registerTool(
    "psecs_raw_update_corp_scripts",
    {
      description: "Update an existing combat script's name and source code.",
      inputSchema: {
        corpId: z.string().describe("Corporation ID"),
        scriptId: z.string().describe("Script ID to update"),
        body: z.string().describe("Updated name and source code (JSON string)"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        corpId: String(args.corpId),
        scriptId: String(args.scriptId),
      };
      let parsedBody: unknown;
      try { parsedBody = JSON.parse(args.body); } catch { return { isError: true, content: [{ type: "text" as const, text: "Invalid JSON in request body" }] }; }
      const result = await client.put("/api/corp/{corpId}/scripts/{scriptId}", parsedBody, options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // DELETE /api/corp/{corpId}/scripts/{scriptId}
  server.registerTool(
    "psecs_raw_delete_corp_scripts",
    {
      description: "Delete a combat script from the corp's library.",
      inputSchema: {
        corpId: z.string().describe("Corporation ID"),
        scriptId: z.string().describe("Script ID to delete"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        corpId: String(args.corpId),
        scriptId: String(args.scriptId),
      };
      const result = await client.delete("/api/corp/{corpId}/scripts/{scriptId}", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/Corp/{id}
  server.registerTool(
    "psecs_raw_corp",
    {
      description: "Get corporation details by ID.",
      inputSchema: {
        id: z.string().describe("Corporation ID"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        id: String(args.id),
      };
      const result = await client.get("/api/Corp/{id}", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/Corp/{corpId}/fleets
  server.registerTool(
    "psecs_raw_corp_fleets",
    {
      description: "Get all fleets owned by a corporation.",
      inputSchema: {
        corpId: z.string().describe("Corporation ID"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        corpId: String(args.corpId),
      };
      const result = await client.get("/api/Corp/{corpId}/fleets", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/Corp/{corpId}/combat-history
  server.registerTool(
    "psecs_raw_corp_combat_history",
    {
      description: "Get paginated combat history for a corporation.",
      inputSchema: {
        corpId: z.string().describe("Corporation ID"),
        page: z.coerce.number().describe("Page number (default 1, minimum 1)").optional(),
        pageSize: z.coerce.number().describe("Items per page (default 20, range 1-100)").optional(),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        corpId: String(args.corpId),
      };
      options.query = {
        page: args.page,
        pageSize: args.pageSize,
      };
      const result = await client.get("/api/Corp/{corpId}/combat-history", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/corp/{corpId}/events
  server.registerTool(
    "psecs_raw_corp_events",
    {
      description: "Query events for a corporation.",
      inputSchema: {
        corpId: z.string().describe("Corporation ID"),
        since: z.string().describe("Return events after this timestamp (optional)").optional(),
        until: z.string().describe("Return events before this timestamp (optional)").optional(),
        type: z.string().describe("Filter by event type, e.g., \"com.psecsapi.fleet.created\" (optional)").optional(),
        source: z.string().describe("Filter by source prefix (optional)").optional(),
        limit: z.coerce.number().describe("Maximum events to return (1-1000, default 100)").optional(),
        cursor: z.string().describe("Pagination cursor from previous response (optional)").optional(),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        corpId: String(args.corpId),
      };
      options.query = {
        since: args.since,
        until: args.until,
        type: args.type,
        source: args.source,
        limit: args.limit,
        cursor: args.cursor,
      };
      const result = await client.get("/api/corp/{corpId}/events", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/Fleet/{fleetId}
  server.registerTool(
    "psecs_raw_fleet",
    {
      description: "Get fleet details including location, ships, and transit status.",
      inputSchema: {
        fleetId: z.string().describe("Fleet ID"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        fleetId: String(args.fleetId),
      };
      const result = await client.get("/api/Fleet/{fleetId}", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/Fleet/{fleetId}/scan
  server.registerTool(
    "psecs_raw_fleet_scan",
    {
      description: "Perform a basic scan of the fleet's current sector.",
      inputSchema: {
        fleetId: z.string().describe("Fleet ID"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        fleetId: String(args.fleetId),
      };
      const result = await client.get("/api/Fleet/{fleetId}/scan", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/Fleet/{fleetId}/scan/deep
  server.registerTool(
    "psecs_raw_fleet_scan_deep",
    {
      description: "Perform a deep scan of a specific orbital in the sector.",
      inputSchema: {
        fleetId: z.string().describe("Fleet ID"),
        orbital: z.coerce.number().describe("Orbital index to scan (optional)").optional(),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        fleetId: String(args.fleetId),
      };
      options.query = {
        orbital: args.orbital,
      };
      const result = await client.get("/api/Fleet/{fleetId}/scan/deep", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/Fleet/{fleetId}/survey
  server.registerTool(
    "psecs_raw_fleet_survey",
    {
      description: "Survey all fleets visible in the current sector.",
      inputSchema: {
        fleetId: z.string().describe("Fleet ID performing the survey"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        fleetId: String(args.fleetId),
      };
      const result = await client.get("/api/Fleet/{fleetId}/survey", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/Fleet/{fleetId}/scan/fleet/{targetFleetId}
  server.registerTool(
    "psecs_raw_fleet_scan_fleet",
    {
      description: "Scan a specific fleet in the current sector.",
      inputSchema: {
        fleetId: z.string().describe("Fleet ID performing the scan"),
        targetFleetId: z.string().describe("Target fleet ID to scan"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        fleetId: String(args.fleetId),
        targetFleetId: String(args.targetFleetId),
      };
      const result = await client.get("/api/Fleet/{fleetId}/scan/fleet/{targetFleetId}", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // PUT /api/Fleet/{fleetId}/enqueue
  server.registerTool(
    "psecs_raw_update_fleet_enqueue",
    {
      description: "Queue the fleet for transit through a conduit.",
      inputSchema: {
        fleetId: z.string().describe("Fleet ID"),
        body: z.string().describe("Target conduit ID (JSON string)"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        fleetId: String(args.fleetId),
      };
      let parsedBody: unknown;
      try { parsedBody = JSON.parse(args.body); } catch { return { isError: true, content: [{ type: "text" as const, text: "Invalid JSON in request body" }] }; }
      const result = await client.put("/api/Fleet/{fleetId}/enqueue", parsedBody, options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // PUT /api/Fleet/{fleetId}/dequeue
  server.registerTool(
    "psecs_raw_update_fleet_dequeue",
    {
      description: "Remove the fleet from the conduit queue.",
      inputSchema: {
        fleetId: z.string().describe("Fleet ID"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        fleetId: String(args.fleetId),
      };
      const result = await client.put("/api/Fleet/{fleetId}/dequeue", undefined, options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // PUT /api/Fleet/{fleetId}/combat-script
  server.registerTool(
    "psecs_raw_update_fleet_combat_script",
    {
      description: "Assign a combat script to a fleet.",
      inputSchema: {
        fleetId: z.string().describe("Fleet ID"),
        body: z.string().describe("Script assignment request (JSON string)"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        fleetId: String(args.fleetId),
      };
      let parsedBody: unknown;
      try { parsedBody = JSON.parse(args.body); } catch { return { isError: true, content: [{ type: "text" as const, text: "Invalid JSON in request body" }] }; }
      const result = await client.put("/api/Fleet/{fleetId}/combat-script", parsedBody, options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // DELETE /api/Fleet/{fleetId}/combat-script
  server.registerTool(
    "psecs_raw_delete_fleet_combat_script",
    {
      description: "Remove the combat script assignment from a fleet.",
      inputSchema: {
        fleetId: z.string().describe("Fleet ID"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        fleetId: String(args.fleetId),
      };
      const result = await client.delete("/api/Fleet/{fleetId}/combat-script", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/corp/{corpId}/Inventory
  server.registerTool(
    "psecs_raw_corp_inventory",
    {
      description: "Get corp-wide inventory with totals and fleet breakdown.",
      inputSchema: {
        corpId: z.string().describe("Corporation ID"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        corpId: String(args.corpId),
      };
      const result = await client.get("/api/corp/{corpId}/Inventory", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/corp/{corpId}/Inventory/fleet/{fleetId}
  server.registerTool(
    "psecs_raw_corp_inventory_fleet",
    {
      description: "Get inventory for a specific fleet with ship breakdown.",
      inputSchema: {
        corpId: z.string().describe("Corporation ID"),
        fleetId: z.string().describe("Fleet ID"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        corpId: String(args.corpId),
        fleetId: String(args.fleetId),
      };
      const result = await client.get("/api/corp/{corpId}/Inventory/fleet/{fleetId}", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/corp/{corpId}/Inventory/ship/{shipId}
  server.registerTool(
    "psecs_raw_corp_inventory_ship",
    {
      description: "Get inventory for a specific ship with cargo hold details.",
      inputSchema: {
        corpId: z.string().describe("Corporation ID"),
        shipId: z.string().describe("Ship ID"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        corpId: String(args.corpId),
        shipId: String(args.shipId),
      };
      const result = await client.get("/api/corp/{corpId}/Inventory/ship/{shipId}", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/sector/{sectorId}/loot
  server.registerTool(
    "psecs_raw_sector_loot",
    {
      description: "List all loot fields in a sector. Includes both exclusive (victor-only) and public loot fields.",
      inputSchema: {
        sectorId: z.string().describe("Sector ID to query"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        sectorId: String(args.sectorId),
      };
      const result = await client.get("/api/sector/{sectorId}/loot", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // POST /api/sector/{sectorId}/loot/{lootId}/pickup
  server.registerTool(
    "psecs_raw_create_sector_loot_pickup",
    {
      description: "Pick up loot from a loot field. During the first hour after combat, only the victor's corp\r\ncan pick up loot. After that, any corp with a fleet in the sector can pick up.\r\nLoot despawns after 24 hours.",
      inputSchema: {
        sectorId: z.string().describe("Sector ID containing the loot field"),
        lootId: z.string().describe("Loot field ID to pick up from"),
        body: z.string().describe("Fleet and ship to receive the cargo (reserved for future use) (JSON string)"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        sectorId: String(args.sectorId),
        lootId: String(args.lootId),
      };
      let parsedBody: unknown;
      try { parsedBody = JSON.parse(args.body); } catch { return { isError: true, content: [{ type: "text" as const, text: "Invalid JSON in request body" }] }; }
      const result = await client.post("/api/sector/{sectorId}/loot/{lootId}/pickup", parsedBody, options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // POST /api/Manufacturing/start
  server.registerTool(
    "psecs_raw_create_manufacturing_start",
    {
      description: "Starts a new manufacturing job on a ship.",
      inputSchema: {
        body: z.string().describe("Job parameters (JSON string)"),
      },
    },
    async (args) => {
      let parsedBody: unknown;
      try { parsedBody = JSON.parse(args.body); } catch { return { isError: true, content: [{ type: "text" as const, text: "Invalid JSON in request body" }] }; }
      const result = await client.post("/api/Manufacturing/start", parsedBody, undefined);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // POST /api/Manufacturing/pause
  server.registerTool(
    "psecs_raw_create_manufacturing_pause",
    {
      description: "Pauses an active manufacturing job.",
      inputSchema: {
        body: z.string().describe("Job ID to pause (JSON string)"),
      },
    },
    async (args) => {
      let parsedBody: unknown;
      try { parsedBody = JSON.parse(args.body); } catch { return { isError: true, content: [{ type: "text" as const, text: "Invalid JSON in request body" }] }; }
      const result = await client.post("/api/Manufacturing/pause", parsedBody, undefined);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // POST /api/Manufacturing/resume
  server.registerTool(
    "psecs_raw_create_manufacturing_resume",
    {
      description: "Resumes a paused manufacturing job.",
      inputSchema: {
        body: z.string().describe("Job ID to resume (JSON string)"),
      },
    },
    async (args) => {
      let parsedBody: unknown;
      try { parsedBody = JSON.parse(args.body); } catch { return { isError: true, content: [{ type: "text" as const, text: "Invalid JSON in request body" }] }; }
      const result = await client.post("/api/Manufacturing/resume", parsedBody, undefined);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // POST /api/Manufacturing/cancel
  server.registerTool(
    "psecs_raw_create_manufacturing_cancel",
    {
      description: "Cancels a manufacturing job. Completed items are kept.",
      inputSchema: {
        body: z.string().describe("Job ID to cancel (JSON string)"),
      },
    },
    async (args) => {
      let parsedBody: unknown;
      try { parsedBody = JSON.parse(args.body); } catch { return { isError: true, content: [{ type: "text" as const, text: "Invalid JSON in request body" }] }; }
      const result = await client.post("/api/Manufacturing/cancel", parsedBody, undefined);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/Manufacturing/status
  server.registerTool(
    "psecs_raw_manufacturing_status",
    {
      description: "Gets manufacturing status for the corp, optionally filtered by ship.",
      inputSchema: {
        shipId: z.string().describe("Optional ship ID filter").optional(),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.query = {
        shipId: args.shipId,
      };
      const result = await client.get("/api/Manufacturing/status", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/Manufacturing/blueprints
  server.registerTool(
    "psecs_raw_manufacturing_blueprints",
    {
      description: "Gets list of blueprints owned by the corp.",
    },
    async () => {
      const result = await client.get("/api/Manufacturing/blueprints", undefined);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/Manufacturing/blueprint/{blueprintId}
  server.registerTool(
    "psecs_raw_manufacturing_blueprint",
    {
      description: "Gets detailed information about a specific blueprint definition.",
      inputSchema: {
        blueprintId: z.string().describe("Blueprint definition ID to inspect"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        blueprintId: String(args.blueprintId),
      };
      const result = await client.get("/api/Manufacturing/blueprint/{blueprintId}", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/market
  server.registerTool(
    "psecs_raw_market",
    {
      description: "List market listings with optional filters.",
      inputSchema: {
        type: z.string().describe("Filter by sale type (buynow, auction)").optional(),
        minPrice: z.coerce.number().describe("Minimum price filter").optional(),
        maxPrice: z.coerce.number().describe("Maximum price filter").optional(),
        assetType: z.string().describe("Filter by asset type").optional(),
        seller: z.string().describe("Filter by seller corp ID").optional(),
        endingSoon: z.string().describe("Filter by time remaining (e.g., \"1d\", \"6h\")").optional(),
        sort: z.string().describe("Sort field (price, time, newest, bids)").optional(),
        desc: z.coerce.boolean().describe("Sort descending (default true)").optional(),
        page: z.coerce.number().describe("Page number (default 1)").optional(),
        limit: z.coerce.number().describe("Page size (default 20)").optional(),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.query = {
        type: args.type,
        minPrice: args.minPrice,
        maxPrice: args.maxPrice,
        assetType: args.assetType,
        seller: args.seller,
        endingSoon: args.endingSoon,
        sort: args.sort,
        desc: args.desc,
        page: args.page,
        limit: args.limit,
      };
      const result = await client.get("/api/market", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // POST /api/market
  server.registerTool(
    "psecs_raw_create_market",
    {
      description: "Create a new sale listing.",
      inputSchema: {
        body: z.string().describe("Sale creation parameters (JSON string)"),
      },
    },
    async (args) => {
      let parsedBody: unknown;
      try { parsedBody = JSON.parse(args.body); } catch { return { isError: true, content: [{ type: "text" as const, text: "Invalid JSON in request body" }] }; }
      const result = await client.post("/api/market", parsedBody, undefined);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/market/{saleId}
  server.registerTool(
    "psecs_raw_market_by_sale",
    {
      description: "Get sale details by ID.",
      inputSchema: {
        saleId: z.string().describe("Sale ID"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        saleId: String(args.saleId),
      };
      const result = await client.get("/api/market/{saleId}", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // POST /api/market/{saleId}/repost
  server.registerTool(
    "psecs_raw_create_market_repost",
    {
      description: "Repost an expired or cancelled sale.",
      inputSchema: {
        saleId: z.string().describe("Original sale ID"),
        body: z.string().describe("New sale parameters (JSON string)"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        saleId: String(args.saleId),
      };
      let parsedBody: unknown;
      try { parsedBody = JSON.parse(args.body); } catch { return { isError: true, content: [{ type: "text" as const, text: "Invalid JSON in request body" }] }; }
      const result = await client.post("/api/market/{saleId}/repost", parsedBody, options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // POST /api/market/{saleId}/purchase
  server.registerTool(
    "psecs_raw_create_market_purchase",
    {
      description: "Purchase a Buy Now sale.",
      inputSchema: {
        saleId: z.string().describe("Sale ID to purchase"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        saleId: String(args.saleId),
      };
      const result = await client.post("/api/market/{saleId}/purchase", undefined, options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // POST /api/market/{saleId}/bid
  server.registerTool(
    "psecs_raw_create_market_bid",
    {
      description: "Place a bid on an auction.",
      inputSchema: {
        saleId: z.string().describe("Auction sale ID"),
        body: z.string().describe("Bid amount (JSON string)"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        saleId: String(args.saleId),
      };
      let parsedBody: unknown;
      try { parsedBody = JSON.parse(args.body); } catch { return { isError: true, content: [{ type: "text" as const, text: "Invalid JSON in request body" }] }; }
      const result = await client.post("/api/market/{saleId}/bid", parsedBody, options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // POST /api/market/{saleId}/cancel
  server.registerTool(
    "psecs_raw_create_market_cancel",
    {
      description: "Cancel a sale.",
      inputSchema: {
        saleId: z.string().describe("Sale ID to cancel"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        saleId: String(args.saleId),
      };
      const result = await client.post("/api/market/{saleId}/cancel", undefined, options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // POST /api/market/{saleId}/retrieve
  server.registerTool(
    "psecs_raw_create_market_retrieve",
    {
      description: "Retrieve a purchased or unsold item.",
      inputSchema: {
        saleId: z.string().describe("Sale ID"),
        body: z.string().describe("Ship and cargo module for delivery (JSON string)"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        saleId: String(args.saleId),
      };
      let parsedBody: unknown;
      try { parsedBody = JSON.parse(args.body); } catch { return { isError: true, content: [{ type: "text" as const, text: "Invalid JSON in request body" }] }; }
      const result = await client.post("/api/market/{saleId}/retrieve", parsedBody, options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/market/my-sales
  server.registerTool(
    "psecs_raw_market_my_sales",
    {
      description: "Get current user's sales.",
      inputSchema: {
        state: z.string().describe("Filter by sale state (open, closed, expired, cancelled)").optional(),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.query = {
        state: args.state,
      };
      const result = await client.get("/api/market/my-sales", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/market/my-bids
  server.registerTool(
    "psecs_raw_market_my_bids",
    {
      description: "Get current user's bids.",
    },
    async () => {
      const result = await client.get("/api/market/my-bids", undefined);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/Research/list
  server.registerTool(
    "psecs_raw_research_list",
    {
      description: "List available research options for the user's corporation.",
      inputSchema: {
        technologies: z.coerce.boolean().describe("Include technologies in the response (default: true)").optional(),
        applications: z.coerce.boolean().describe("Include applications in the response (default: true)").optional(),
        primary: z.string().describe("Filter by primary discipline code (B=Biological, C=Chemical, E=Energy, I=Information, M=Mechanical, P=Physical, S=Social)").optional(),
        secondary: z.string().describe("Filter by secondary discipline code (same codes as primary)").optional(),
        tier: z.coerce.number().describe("Filter by technology tier level (1-7)").optional(),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.query = {
        technologies: args.technologies,
        applications: args.applications,
        primary: args.primary,
        secondary: args.secondary,
        tier: args.tier,
      };
      const result = await client.get("/api/Research/list", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/Research/status
  server.registerTool(
    "psecs_raw_research_status",
    {
      description: "Get current research status for the user's corporation.",
    },
    async () => {
      const result = await client.get("/api/Research/status", undefined);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // POST /api/Research/allocate
  server.registerTool(
    "psecs_raw_create_research_allocate",
    {
      description: "Start or update research allocation for a technology or application.",
      inputSchema: {
        body: z.string().describe("Target ID (technology or application) and allocation percentage (1-100) (JSON string)"),
      },
    },
    async (args) => {
      let parsedBody: unknown;
      try { parsedBody = JSON.parse(args.body); } catch { return { isError: true, content: [{ type: "text" as const, text: "Invalid JSON in request body" }] }; }
      const result = await client.post("/api/Research/allocate", parsedBody, undefined);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // POST /api/Research/stop
  server.registerTool(
    "psecs_raw_create_research_stop",
    {
      description: "Stop research on a technology or application.",
      inputSchema: {
        body: z.string().describe("Target ID of the technology or application to stop researching (JSON string)"),
      },
    },
    async (args) => {
      let parsedBody: unknown;
      try { parsedBody = JSON.parse(args.body); } catch { return { isError: true, content: [{ type: "text" as const, text: "Invalid JSON in request body" }] }; }
      const result = await client.post("/api/Research/stop", parsedBody, undefined);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/Research/modifiers
  server.registerTool(
    "psecs_raw_research_modifiers",
    {
      description: "Get active research modifiers for the user's corporation.",
    },
    async () => {
      const result = await client.get("/api/Research/modifiers", undefined);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/Research/completed
  server.registerTool(
    "psecs_raw_research_completed",
    {
      description: "Get completed research for the user's corporation.",
      inputSchema: {
        technologies: z.coerce.boolean().describe("Include completed technologies in response (default: true)").optional(),
        applications: z.coerce.boolean().describe("Include completed applications in response (default: true)").optional(),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.query = {
        technologies: args.technologies,
        applications: args.applications,
      };
      const result = await client.get("/api/Research/completed", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/Research/disciplines
  server.registerTool(
    "psecs_raw_research_disciplines",
    {
      description: "Get the list of disciplines in the tech tree.",
    },
    async () => {
      const result = await client.get("/api/Research/disciplines", undefined);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/Research/components
  server.registerTool(
    "psecs_raw_research_components",
    {
      description: "Get the list of components defined in the tech tree.",
      inputSchema: {
        tier: z.coerce.number().describe("Filter by tier").optional(),
        category: z.string().describe("Filter by category").optional(),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.query = {
        tier: args.tier,
        category: args.category,
      };
      const result = await client.get("/api/Research/components", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/Research/modules
  server.registerTool(
    "psecs_raw_research_modules",
    {
      description: "Get the list of modules defined in the tech tree.",
      inputSchema: {
        tier: z.coerce.number().describe("Filter by tier").optional(),
        category: z.string().describe("Filter by category").optional(),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.query = {
        tier: args.tier,
        category: args.category,
      };
      const result = await client.get("/api/Research/modules", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/Ship/{shipId}
  server.registerTool(
    "psecs_raw_ship",
    {
      description: "Get ship details including modules and cargo.",
      inputSchema: {
        shipId: z.string().describe("Ship ID"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        shipId: String(args.shipId),
      };
      const result = await client.get("/api/Ship/{shipId}", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // POST /api/Ship/{shipId}/extraction
  server.registerTool(
    "psecs_raw_create_ship_extraction",
    {
      description: "Start a resource extraction job on the ship.",
      inputSchema: {
        shipId: z.string().describe("Ship ID"),
        body: z.string().describe("Resource ID and optional quantity limit (JSON string)"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        shipId: String(args.shipId),
      };
      let parsedBody: unknown;
      try { parsedBody = JSON.parse(args.body); } catch { return { isError: true, content: [{ type: "text" as const, text: "Invalid JSON in request body" }] }; }
      const result = await client.post("/api/Ship/{shipId}/extraction", parsedBody, options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // DELETE /api/Ship/{shipId}/extraction
  server.registerTool(
    "psecs_raw_delete_ship_extraction",
    {
      description: "Stop an extraction job and collect extracted resources.",
      inputSchema: {
        shipId: z.string().describe("Ship ID"),
        jobId: z.string().describe("Specific job ID to stop (optional)").optional(),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        shipId: String(args.shipId),
      };
      options.query = {
        jobId: args.jobId,
      };
      const result = await client.delete("/api/Ship/{shipId}/extraction", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/Ship/{shipId}/extraction
  server.registerTool(
    "psecs_raw_ship_extraction",
    {
      description: "Get status of all active extraction jobs on the ship.",
      inputSchema: {
        shipId: z.string().describe("Ship ID"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        shipId: String(args.shipId),
      };
      const result = await client.get("/api/Ship/{shipId}/extraction", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // DELETE /api/Ship/{shipId}/extraction/all
  server.registerTool(
    "psecs_raw_delete_ship_extraction_all",
    {
      description: "Stop all extraction jobs on the ship and collect resources.",
      inputSchema: {
        shipId: z.string().describe("Ship ID"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        shipId: String(args.shipId),
      };
      const result = await client.delete("/api/Ship/{shipId}/extraction/all", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // POST /api/Ship/{shipId}/install
  server.registerTool(
    "psecs_raw_create_ship_install",
    {
      description: "Install modules onto a ship from boxed module assets.",
      inputSchema: {
        shipId: z.string().describe("Ship ID"),
        body: z.string().describe("List of boxed module IDs to install (JSON string)"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        shipId: String(args.shipId),
      };
      let parsedBody: unknown;
      try { parsedBody = JSON.parse(args.body); } catch { return { isError: true, content: [{ type: "text" as const, text: "Invalid JSON in request body" }] }; }
      const result = await client.post("/api/Ship/{shipId}/install", parsedBody, options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // POST /api/Ship/{shipId}/uninstall
  server.registerTool(
    "psecs_raw_create_ship_uninstall",
    {
      description: "Uninstall modules from a ship into boxed assets in cargo.",
      inputSchema: {
        shipId: z.string().describe("Ship ID"),
        body: z.string().describe("Module IDs to uninstall and destination cargo hold (JSON string)"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        shipId: String(args.shipId),
      };
      let parsedBody: unknown;
      try { parsedBody = JSON.parse(args.body); } catch { return { isError: true, content: [{ type: "text" as const, text: "Invalid JSON in request body" }] }; }
      const result = await client.post("/api/Ship/{shipId}/uninstall", parsedBody, options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/Ship/{shipId}/cargo
  server.registerTool(
    "psecs_raw_ship_cargo",
    {
      description: "List all items in a ship's cargo hold.",
      inputSchema: {
        shipId: z.string(),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        shipId: String(args.shipId),
      };
      const result = await client.get("/api/Ship/{shipId}/cargo", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/Ship/{shipId}/cargo/{assetId}/inspect
  server.registerTool(
    "psecs_raw_ship_cargo_inspect",
    {
      description: "Inspects a boxed asset in ship cargo, showing detailed properties.",
      inputSchema: {
        shipId: z.string().describe("Ship ID"),
        assetId: z.string().describe("Boxed asset ID to inspect"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        shipId: String(args.shipId),
        assetId: String(args.assetId),
      };
      const result = await client.get("/api/Ship/{shipId}/cargo/{assetId}/inspect", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/ship-catalog
  server.registerTool(
    "psecs_raw_ship_catalog",
    {
      description: "List all ship catalog configurations, optionally filtered by class.",
      inputSchema: {
        class: z.string().describe("Optional ship class filter (e.g. Scout, Corvette)").optional(),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.query = {
        class: args.class,
      };
      const result = await client.get("/api/ship-catalog", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/shipyard/queue
  server.registerTool(
    "psecs_raw_shipyard_queue",
    {
      description: "Get the current shipyard build queue.",
    },
    async () => {
      const result = await client.get("/api/shipyard/queue", undefined);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // POST /api/shipyard/build
  server.registerTool(
    "psecs_raw_create_shipyard_build",
    {
      description: "Place a new chassis build order.",
      inputSchema: {
        body: z.string().describe("Request body as JSON string"),
      },
    },
    async (args) => {
      let parsedBody: unknown;
      try { parsedBody = JSON.parse(args.body); } catch { return { isError: true, content: [{ type: "text" as const, text: "Invalid JSON in request body" }] }; }
      const result = await client.post("/api/shipyard/build", parsedBody, undefined);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // DELETE /api/shipyard/build/{orderNumber}
  server.registerTool(
    "psecs_raw_delete_shipyard_build",
    {
      description: "Cancel a queued build order.",
      inputSchema: {
        orderNumber: z.coerce.number(),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        orderNumber: String(args.orderNumber),
      };
      const result = await client.delete("/api/shipyard/build/{orderNumber}", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/shipyard/blueprint/{blueprintId}
  server.registerTool(
    "psecs_raw_shipyard_blueprint",
    {
      description: "Get details of a chassis blueprint, including resource and component costs.",
      inputSchema: {
        blueprintId: z.string().describe("Chassis blueprint definition ID (e.g., \"scout-chassis\")"),
        interiorSlots: z.coerce.number().describe("Number of interior slots for cost calculation (optional)").optional(),
        exteriorSlots: z.coerce.number().describe("Number of exterior slots for cost calculation (optional)").optional(),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        blueprintId: String(args.blueprintId),
      };
      options.query = {
        interiorSlots: args.interiorSlots,
        exteriorSlots: args.exteriorSlots,
      };
      const result = await client.get("/api/shipyard/blueprint/{blueprintId}", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // POST /api/shipyard/pickup/{orderNumber}
  server.registerTool(
    "psecs_raw_create_shipyard_pickup",
    {
      description: "Pick up a completed chassis and add it to a fleet.",
      inputSchema: {
        orderNumber: z.coerce.number(),
        fleetId: z.string().optional(),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        orderNumber: String(args.orderNumber),
      };
      options.query = {
        fleetId: args.fleetId,
      };
      const result = await client.post("/api/shipyard/pickup/{orderNumber}", undefined, options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // POST /api/Space
  server.registerTool(
    "psecs_raw_create_space",
    {
      description: "Generate new sectors in the game universe.",
      inputSchema: {
        body: z.string().describe("Number of sectors to create (JSON string)"),
      },
    },
    async (args) => {
      let parsedBody: unknown;
      try { parsedBody = JSON.parse(args.body); } catch { return { isError: true, content: [{ type: "text" as const, text: "Invalid JSON in request body" }] }; }
      const result = await client.post("/api/Space", parsedBody, undefined);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/Space/stats
  server.registerTool(
    "psecs_raw_space_stats",
    {
      description: "Get universe and personal map statistics.",
    },
    async () => {
      const result = await client.get("/api/Space/stats", undefined);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // POST /api/tokens/checkout
  server.registerTool(
    "psecs_raw_create_tokens_checkout",
    {
      description: "Create a Stripe Checkout Session for token purchase",
      inputSchema: {
        body: z.string().describe("Request body as JSON string"),
      },
    },
    async (args) => {
      let parsedBody: unknown;
      try { parsedBody = JSON.parse(args.body); } catch { return { isError: true, content: [{ type: "text" as const, text: "Invalid JSON in request body" }] }; }
      const result = await client.post("/api/tokens/checkout", parsedBody, undefined);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/tokens/balance
  server.registerTool(
    "psecs_raw_tokens_balance",
    {
      description: "Get current token balance and staking info",
    },
    async () => {
      const result = await client.get("/api/tokens/balance", undefined);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/tokens/purchases
  server.registerTool(
    "psecs_raw_tokens_purchases",
    {
      description: "Get purchase history for the authenticated user (EM correction #3)",
    },
    async () => {
      const result = await client.get("/api/tokens/purchases", undefined);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/User
  server.registerTool(
    "psecs_raw_user",
    {
      description: "Get the authenticated user's profile.",
    },
    async () => {
      const result = await client.get("/api/User", undefined);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // POST /api/User/corp
  server.registerTool(
    "psecs_raw_create_user_corp",
    {
      description: "Create a new corporation for the authenticated user.",
      inputSchema: {
        body: z.string().describe("Corporation name (JSON string)"),
      },
    },
    async (args) => {
      let parsedBody: unknown;
      try { parsedBody = JSON.parse(args.body); } catch { return { isError: true, content: [{ type: "text" as const, text: "Invalid JSON in request body" }] }; }
      const result = await client.post("/api/User/corp", parsedBody, undefined);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // POST /api/User/stake-api-tokens
  server.registerTool(
    "psecs_raw_create_user_stake_api_tokens",
    {
      description: "Stake tokens to increase API rate limit.",
      inputSchema: {
        body: z.string().describe("Amount of tokens to stake (JSON string)"),
      },
    },
    async (args) => {
      let parsedBody: unknown;
      try { parsedBody = JSON.parse(args.body); } catch { return { isError: true, content: [{ type: "text" as const, text: "Invalid JSON in request body" }] }; }
      const result = await client.post("/api/User/stake-api-tokens", parsedBody, undefined);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // POST /api/User/unstake-api-tokens
  server.registerTool(
    "psecs_raw_create_user_unstake_api_tokens",
    {
      description: "Unstake tokens from API rate limit stake.",
      inputSchema: {
        body: z.string().describe("Amount of tokens to unstake (JSON string)"),
      },
    },
    async (args) => {
      let parsedBody: unknown;
      try { parsedBody = JSON.parse(args.body); } catch { return { isError: true, content: [{ type: "text" as const, text: "Invalid JSON in request body" }] }; }
      const result = await client.post("/api/User/unstake-api-tokens", parsedBody, undefined);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/User/api-stake-info
  server.registerTool(
    "psecs_raw_user_api_stake_info",
    {
      description: "Get current API staking information.",
    },
    async () => {
      const result = await client.get("/api/User/api-stake-info", undefined);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/UserMap
  server.registerTool(
    "psecs_raw_usermap",
    {
      description: "Get the user's known sectors.",
      inputSchema: {
        type: z.string().describe("Sector type filter: Void, Nebula, Rubble, StarSystem, BlackHole, Nexus, Favorites, or * for all").optional(),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.query = {
        type: args.type,
      };
      const result = await client.get("/api/UserMap", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // GET /api/UserMap/{sectorId}
  server.registerTool(
    "psecs_raw_usermap_by_sector",
    {
      description: "Get details for a specific sector from the user's map.",
      inputSchema: {
        sectorId: z.string().describe("Sector ID"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        sectorId: String(args.sectorId),
      };
      const result = await client.get("/api/UserMap/{sectorId}", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // POST /api/UserMap/{sectorId}/favorite
  server.registerTool(
    "psecs_raw_create_usermap_favorite",
    {
      description: "Mark a sector as a favorite.",
      inputSchema: {
        sectorId: z.string().describe("Sector ID"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        sectorId: String(args.sectorId),
      };
      const result = await client.post("/api/UserMap/{sectorId}/favorite", undefined, options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // DELETE /api/UserMap/{sectorId}/favorite
  server.registerTool(
    "psecs_raw_delete_usermap_favorite",
    {
      description: "Remove a sector from favorites.",
      inputSchema: {
        sectorId: z.string().describe("Sector ID"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        sectorId: String(args.sectorId),
      };
      const result = await client.delete("/api/UserMap/{sectorId}/favorite", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // PUT /api/UserMap/{sectorId}/note
  server.registerTool(
    "psecs_raw_update_usermap_note",
    {
      description: "Set a personal note on a sector.",
      inputSchema: {
        sectorId: z.string().describe("Sector ID"),
        body: z.string().describe("Note content (max 500 characters) (JSON string)"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        sectorId: String(args.sectorId),
      };
      let parsedBody: unknown;
      try { parsedBody = JSON.parse(args.body); } catch { return { isError: true, content: [{ type: "text" as const, text: "Invalid JSON in request body" }] }; }
      const result = await client.put("/api/UserMap/{sectorId}/note", parsedBody, options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );

  // DELETE /api/UserMap/{sectorId}/note
  server.registerTool(
    "psecs_raw_delete_usermap_note",
    {
      description: "Clear the note from a sector.",
      inputSchema: {
        sectorId: z.string().describe("Sector ID"),
      },
    },
    async (args) => {
      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};
      options.path = {
        sectorId: String(args.sectorId),
      };
      const result = await client.delete("/api/UserMap/{sectorId}/note", options);
      if (!result.ok) return formatToolError(result);
      return formatToolResult(result.data);
    },
  );
}
