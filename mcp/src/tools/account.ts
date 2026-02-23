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
}
