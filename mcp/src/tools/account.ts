import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { PsecsClient } from "../client.js";
import { formatToolResult, formatToolError } from "../tool-utils.js";

interface UserProfile {
  entityId?: string;
  name?: string;
  ownedCorps?: string[];
  tokens?: number;
  [key: string]: unknown;
}

interface CorpDetails {
  entityId?: string;
  name?: string;
  credits?: number;
  [key: string]: unknown;
}

interface CorpFleetsData {
  corpFleets?: string[];
  [key: string]: unknown;
}

interface AccountResearchStatus {
  activeProjects?: Array<{ targetId: string; allocationPercent: number; [key: string]: unknown }>;
  totalCapacity?: number;
  totalAllocation?: number;
  [key: string]: unknown;
}

interface ManufacturingStatus {
  jobs?: Array<{ jobId: string; [key: string]: unknown }>;
  [key: string]: unknown;
}

export function registerAccountTools(
  server: McpServer,
  client: PsecsClient
): void {
  server.registerTool(
    "psecs_account_overview",
    {
      description:
        "Get a comprehensive overview of the player's game state including user profile, " +
        "corp details, fleets, research status, and manufacturing status. " +
        "Returns strategy suggestions and warnings.",
    },
    async () => {
      const suggestions: string[] = [];
      const warnings: string[] = [];

      // Step 1: Get user profile
      const userResult = await client.get<UserProfile>("/api/User");
      if (!userResult.ok) return formatToolError(userResult);
      const user = userResult.data;

      // If no corp, return early with suggestion
      if (!user.ownedCorps || user.ownedCorps.length === 0) {
        suggestions.push(
          "No corp yet — create one with psecs_create_corp to start playing."
        );
        return formatToolResult({ user, suggestions, warnings });
      }

      // Single-corp-per-user model: use the first (and only) corp.
      const corpId = user.ownedCorps[0];

      // Step 2: Parallel fetch corp details, fleets, research, and manufacturing
      const [corpResult, fleetsResult, researchResult, manufacturingResult] =
        await Promise.all([
          client.get<CorpDetails>("/api/Corp/{id}", {
            path: { id: corpId },
          }),
          client.get<CorpFleetsData>("/api/Corp/{corpId}/fleets", {
            path: { corpId },
          }),
          client.get<AccountResearchStatus>("/api/Research/status"),
          client.get<ManufacturingStatus>("/api/Manufacturing/status"),
        ]);

      const corp = corpResult.ok ? corpResult.data : null;
      const fleetsData = fleetsResult.ok ? fleetsResult.data : null;
      const fleetIds = fleetsData?.corpFleets ?? [];
      const research = researchResult.ok ? researchResult.data : null;
      const manufacturing = manufacturingResult.ok
        ? manufacturingResult.data
        : null;

      if (!corpResult.ok) warnings.push("Could not fetch corp details.");
      if (!fleetsResult.ok) warnings.push("Could not fetch fleet list.");
      if (!researchResult.ok) warnings.push("Could not fetch research status.");
      if (!manufacturingResult.ok)
        warnings.push("Could not fetch manufacturing status.");

      // Generate suggestions
      if (fleetsData && fleetIds.length === 0) {
        suggestions.push(
          "You have no fleets. Build a ship at the shipyard to get started."
        );
      }

      if (research) {
        const totalCapacity = research.totalCapacity ?? 0;
        const totalAllocation = research.totalAllocation ?? 0;
        if (totalCapacity === 0) {
          suggestions.push(
            "No research capacity — install research modules on a ship to unlock the tech tree."
          );
        } else if (totalAllocation < 100) {
          const pct = 100 - totalAllocation;
          suggestions.push(
            `Research capacity ${pct}% unallocated — use psecs_research_overview to find techs to research.`
          );
        }
      }

      if (manufacturing) {
        const jobs = manufacturing.jobs ?? [];
        if (jobs.length === 0) {
          suggestions.push(
            "No active manufacturing jobs. Use psecs_manufacturing_overview to see available blueprints and queue a production run."
          );
        }
      }

      if (corp && (corp.credits ?? 0) === 0) {
        suggestions.push(
          "Corp has zero credits. Sell resources on the Nexus Market to earn credits."
        );
      }

      return formatToolResult({
        user,
        corp,
        fleetIds,
        fleetCount: fleetIds.length,
        research,
        manufacturing,
        suggestions,
        warnings,
      });
    }
  );

  server.registerTool(
    "psecs_create_corp",
    {
      description:
        "Create a new corporation for the authenticated user. " +
        "A corporation is required to play the game — it owns your fleets, ships, and credits.",
      inputSchema: {
        name: z
          .string()
          .describe("Name for the new corporation"),
      },
    },
    async (args) => {
      const suggestions: string[] = [];
      const warnings: string[] = [];

      const result = await client.post("/api/User/corp", { name: args.name });
      if (!result.ok) return formatToolError(result);

      suggestions.push(
        "Corp created! Next steps: build a ship at the shipyard, then explore sectors."
      );
      suggestions.push(
        "Use psecs_account_overview to see your full game state."
      );

      return formatToolResult({
        ...result.data as Record<string, unknown>,
        suggestions,
        warnings,
      });
    }
  );

  server.registerTool(
    "psecs_corp_events",
    {
      description:
        "Query the corp event log to see what has happened recently. " +
        "Events cover: extraction completions, manufacturing job starts/completions/cancellations, " +
        "market sales (purchased, won auction, outbid), warehouse deposits/withdrawals, fleet creation, " +
        "and more. Use this to drive event-based agent loops — poll after waiting to detect what changed. " +
        "Returns action-oriented suggestions based on event types found.",
      inputSchema: {
        limit: z
          .number()
          .min(1)
          .max(1000)
          .default(50)
          .describe("Maximum events to return (default 50)"),
        since: z
          .string()
          .optional()
          .describe(
            "Return only events after this ISO 8601 timestamp (e.g. 2025-01-15T14:00:00Z). " +
            "Store the most recent event time between polls to avoid reprocessing."
          ),
        type: z
          .string()
          .optional()
          .describe(
            "Filter by event type prefix (e.g. 'com.psecsapi.manufacturing' to get all manufacturing events, " +
            "or 'com.psecsapi.sale.purchased' for exact match). Common types: " +
            "com.psecsapi.manufacturing.job.completed, com.psecsapi.sale.purchased, " +
            "com.psecsapi.auction.won, com.psecsapi.bid.outbid, " +
            "com.psecsapi.warehouse.deposit, com.psecsapi.warehouse.withdraw"
          ),
        cursor: z
          .string()
          .optional()
          .describe("Pagination cursor from a previous response for loading more events"),
      },
    },
    async (args) => {
      const suggestions: string[] = [];
      const warnings: string[] = [];

      // Discover corp ID from user profile
      const userResult = await client.get<UserProfile>("/api/User");
      if (!userResult.ok) return formatToolError(userResult);
      const user = userResult.data;

      if (!user.ownedCorps || user.ownedCorps.length === 0) {
        return formatToolResult({
          eventCount: 0,
          events: [],
          cursor: null,
          warnings: ["No corporation found. Create one with psecs_create_corp to start playing."],
          suggestions: [],
        });
      }

      const corpId = user.ownedCorps[0];

      interface EventsResponse {
        events?: Array<{
          type?: string;
          time?: string;
          source?: string;
          id?: string;
          data?: Record<string, unknown>;
          [key: string]: unknown;
        }>;
        cursor?: string | null;
        [key: string]: unknown;
      }

      const queryOptions: { query?: Record<string, string | number> } = { query: {} };
      queryOptions.query = {};
      if (args.limit !== undefined) queryOptions.query["limit"] = args.limit;
      if (args.since) queryOptions.query["since"] = args.since;
      if (args.type) queryOptions.query["type"] = args.type;
      if (args.cursor) queryOptions.query["cursor"] = args.cursor;

      const result = await client.get<EventsResponse>(
        "/api/corp/{corpId}/events",
        { path: { corpId }, ...queryOptions }
      );
      if (!result.ok) return formatToolError(result);

      const events = result.data.events ?? [];
      const nextCursor = result.data.cursor;

      // Generate contextual suggestions from event types found
      const eventTypes = new Set(events.map((e) => e.type ?? ""));

      if (eventTypes.has("com.psecsapi.manufacturing.job.completed")) {
        suggestions.push(
          "Manufacturing job(s) completed — use psecs_manufacturing_overview to see output in cargo and queue next job."
        );
      }
      if (eventTypes.has("com.psecsapi.manufacturing.job.cancelled")) {
        warnings.push(
          "Manufacturing job(s) cancelled — check psecs_manufacturing_overview for reason (insufficient resources or cargo space)."
        );
      }
      if (eventTypes.has("com.psecsapi.sale.purchased")) {
        suggestions.push(
          "Sale(s) completed — use psecs_account_overview to check updated credit balance."
        );
      }
      if (eventTypes.has("com.psecsapi.auction.won")) {
        suggestions.push(
          "Auction won — use psecs_market_portfolio to check pickup window and retrieve the asset before it expires."
        );
      }
      if (eventTypes.has("com.psecsapi.bid.outbid")) {
        warnings.push(
          "You were outbid on an auction. Use psecs_market_search to check current price and decide whether to rebid."
        );
      }
      if (eventTypes.has("com.psecsapi.sale.expired")) {
        warnings.push(
          "A sale listing expired without selling. Consider relisting at a lower price."
        );
      }

      if (events.length === 0) {
        suggestions.push(
          "No events found for the given filters. Try a wider time range or remove the type filter."
        );
      } else if (nextCursor) {
        suggestions.push(
          `More events available — pass cursor '${nextCursor}' to load the next page.`
        );
      }

      return formatToolResult({
        corpId,
        eventCount: events.length,
        events,
        cursor: nextCursor ?? null,
        suggestions,
        warnings,
      });
    }
  );
}
