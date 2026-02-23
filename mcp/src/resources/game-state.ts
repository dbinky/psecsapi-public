import {
  McpServer,
  ResourceTemplate,
} from "@modelcontextprotocol/sdk/server/mcp.js";
import { PsecsClient } from "../client.js";
import type {
  UserProfileResponseModel,
  ResearchStatusResponseModel,
  ActiveProjectModel,
  ManufacturingStatusResponseModel,
  ManufacturingJobModel,
  MyBidsItemModel,
  MarketListingItemModel,
  MarketListingResponseModel,
  CorpInventoryResponseModel,
  ResourceTotalModel,
  FleetDetailResponseModel,
  ShipDetailResponseModel,
} from "../generated/types.js";

export function registerGameStateResources(
  server: McpServer,
  client: PsecsClient
): void {
  // --- Static URI resources for aggregate state ---

  server.registerResource(
    "account-state",
    "psecs://state/account",
    {
      description:
        "Current account state — profile, corp, credits, and strategy hints",
    },
    async (uri) => {
      const hints: string[] = [];
      const userResult = await client.get<UserProfileResponseModel>("/api/User");
      if (!userResult.ok) {
        return errorResource(
          uri.href,
          "Failed to fetch account: " + userResult.message
        );
      }
      const user = userResult.data;

      // Corp endpoint returns more fields than the OpenAPI spec documents
      // (e.g. credits), so we keep Record<string, unknown> here.
      let corp: Record<string, unknown> | null = null;
      if (user.ownedCorps && user.ownedCorps.length > 0) {
        // ownedCorps[0]: The game enforces a single-corp-per-user model.
        // This mirrors the API controller pattern (e.g. CorpController uses
        // the first owned corp). If multi-corp support is ever added, this
        // and the API controllers would need to change together.
        const corpResult = await client.get<Record<string, unknown>>(
          "/api/Corp/{id}",
          { path: { id: user.ownedCorps[0] } }
        );
        if (corpResult.ok) {
          corp = corpResult.data;
          if (((corp.credits as number) ?? 0) < 100) {
            hints.push(
              "Low credits — consider selling resources on the market."
            );
          }
        }
      } else {
        hints.push(
          "No corporation yet. Use psecs_create_corp to start playing."
        );
      }

      return mdResource(uri.href, formatAccountState(user, corp, hints));
    }
  );

  server.registerResource(
    "fleets-state",
    "psecs://state/fleets",
    {
      description: "All fleet positions, statuses, and navigation hints",
    },
    async (uri) => {
      const userResult = await client.get<UserProfileResponseModel>("/api/User");
      if (!userResult.ok) {
        return errorResource(
          uri.href,
          "Failed to fetch user: " + userResult.message
        );
      }
      const user = userResult.data;
      if (!user.ownedCorps || user.ownedCorps.length === 0) {
        return mdResource(uri.href, "# Fleets\nNo corporation yet.");
      }

      // The /api/Corp/{corpId}/fleets endpoint returns { corpFleets: Guid[] } —
      // only fleet IDs, not fleet detail objects. For per-fleet status use
      // GET /api/Fleet/{fleetId} individually or the psecs://fleet/{id}/status resource.
      const fleetsResult = await client.get<{ corpFleets?: string[] }>(
        "/api/Corp/{corpId}/fleets",
        { path: { corpId: user.ownedCorps[0] } }
      );
      if (!fleetsResult.ok) {
        return errorResource(
          uri.href,
          "Failed to fetch fleets: " + fleetsResult.message
        );
      }

      const hints: string[] = [];
      const fleetIds = fleetsResult.data.corpFleets ?? [];
      if (fleetIds.length === 0) {
        hints.push("No fleets — build a ship at the shipyard to get started.");
      } else {
        hints.push(
          `Use psecs_fleet_status or psecs://fleet/{fleetId}/status for detailed fleet state.`
        );
      }

      return mdResource(uri.href, formatFleetsState(fleetIds, hints));
    }
  );

  server.registerResource(
    "research-state",
    "psecs://state/research",
    {
      description: "Research allocations, progress, and optimization hints",
    },
    async (uri) => {
      const result = await client.get<ResearchStatusResponseModel>(
        "/api/Research/status"
      );
      if (!result.ok) {
        return errorResource(
          uri.href,
          "Failed to fetch research: " + result.message
        );
      }

      const hints: string[] = [];
      const data = result.data;
      const total = data.totalCapacity ?? 0;
      const allocated = data.totalAllocation ?? 0;
      if (total > 0 && allocated < 100) {
        hints.push(
          `${100 - allocated}% research capacity unallocated — allocate it!`
        );
      }
      if (total === 0) {
        hints.push(
          "No research capacity — install research modules on a ship."
        );
      }

      return mdResource(uri.href, formatResearchState(data, hints));
    }
  );

  server.registerResource(
    "manufacturing-state",
    "psecs://state/manufacturing",
    {
      description: "Manufacturing queue, capacity, and production hints",
    },
    async (uri) => {
      const result = await client.get<ManufacturingStatusResponseModel>(
        "/api/Manufacturing/status"
      );
      if (!result.ok) {
        return errorResource(
          uri.href,
          "Failed to fetch manufacturing: " + result.message
        );
      }

      const hints: string[] = [];
      const data = result.data;
      const jobs = data.jobs ?? [];
      if (jobs.length === 0) {
        hints.push(
          "No active manufacturing jobs — check blueprints for items to produce."
        );
      }
      const pausedJobs = jobs.filter((j) => j.status === "Paused");
      if (pausedJobs.length > 0) {
        hints.push(
          `${pausedJobs.length} paused job(s) — check for missing resources or full cargo.`
        );
      }

      return mdResource(uri.href, formatManufacturingState(data, hints));
    }
  );

  server.registerResource(
    "market-state",
    "psecs://state/market",
    {
      description: "Active market listings, bids, and trading hints",
    },
    async (uri) => {
      const userResult = await client.get<UserProfileResponseModel>("/api/User");
      if (!userResult.ok) {
        return errorResource(
          uri.href,
          "Failed to fetch user: " + userResult.message
        );
      }
      const user = userResult.data;
      if (!user.ownedCorps || user.ownedCorps.length === 0) {
        return mdResource(uri.href, "# Market\nNo corporation yet.");
      }

      const bidsResult =
        await client.get<MyBidsItemModel[]>("/api/Market/my-bids");
      const salesResult =
        await client.get<MarketListingResponseModel>(
          "/api/Market/my-sales"
        );

      const hints: string[] = [];
      const bids = bidsResult.ok ? bidsResult.data : [];
      const sales = salesResult.ok ? (salesResult.data.listings ?? []) : [];

      if (!bidsResult.ok) {
        hints.push("Warning: could not fetch your bids.");
      }
      if (!salesResult.ok) {
        hints.push("Warning: could not fetch your sales.");
      }
      if (bidsResult.ok && salesResult.ok && bids.length === 0 && sales.length === 0) {
        hints.push(
          "No market activity — consider listing surplus resources for sale."
        );
      }

      return mdResource(uri.href, formatMarketState(bids, sales, hints));
    }
  );

  server.registerResource(
    "inventory-state",
    "psecs://state/inventory",
    {
      description: "Corp-wide resource totals and utilization hints",
    },
    async (uri) => {
      const userResult = await client.get<UserProfileResponseModel>("/api/User");
      if (!userResult.ok) {
        return errorResource(
          uri.href,
          "Failed to fetch user: " + userResult.message
        );
      }
      const user = userResult.data;
      if (!user.ownedCorps || user.ownedCorps.length === 0) {
        return mdResource(uri.href, "# Inventory\nNo corporation yet.");
      }

      const result = await client.get<CorpInventoryResponseModel>(
        "/api/corp/{corpId}/Inventory",
        { path: { corpId: user.ownedCorps[0] } }
      );
      if (!result.ok) {
        return errorResource(
          uri.href,
          "Failed to fetch inventory: " + result.message
        );
      }

      const hints: string[] = [];
      const data = result.data;
      const resources = data.totals ?? [];
      if (resources.length === 0) {
        hints.push(
          "Empty inventory — start extracting resources from orbital bodies."
        );
      }

      return mdResource(uri.href, formatInventoryState(data, hints));
    }
  );

  // --- Parameterized templates ---

  server.registerResource(
    "fleet-detail",
    new ResourceTemplate("psecs://fleet/{fleetId}/status", {
      list: undefined,
    }),
    { description: "Detailed status of a specific fleet" },
    async (uri, { fleetId }) => {
      const id = Array.isArray(fleetId) ? fleetId[0] : fleetId;
      const result = await client.get<FleetDetailResponseModel>(
        "/api/Fleet/{fleetId}",
        { path: { fleetId: id } }
      );
      if (!result.ok) {
        return errorResource(
          uri.href,
          "Failed to fetch fleet: " + result.message
        );
      }
      return mdResource(
        uri.href,
        `# Fleet ${id}\n\n\`\`\`json\n${JSON.stringify(result.data, null, 2)}\n\`\`\``
      );
    }
  );

  server.registerResource(
    "ship-detail",
    new ResourceTemplate("psecs://ship/{shipId}/status", {
      list: undefined,
    }),
    { description: "Detailed status of a specific ship" },
    async (uri, { shipId }) => {
      const id = Array.isArray(shipId) ? shipId[0] : shipId;
      const result = await client.get<ShipDetailResponseModel>(
        "/api/Ship/{id}",
        { path: { id } }
      );
      if (!result.ok) {
        return errorResource(
          uri.href,
          "Failed to fetch ship: " + result.message
        );
      }
      return mdResource(
        uri.href,
        `# Ship ${id}\n\n\`\`\`json\n${JSON.stringify(result.data, null, 2)}\n\`\`\``
      );
    }
  );
}

// --- Helpers ---

function mdResource(uri: string, text: string) {
  return { contents: [{ uri, mimeType: "text/markdown" as const, text }] };
}

function errorResource(uri: string, message: string) {
  return {
    contents: [
      { uri, mimeType: "text/markdown" as const, text: `# Error\n\n${message}` },
    ],
  };
}

// --- Formatters ---

function formatAccountState(
  user: UserProfileResponseModel,
  corp: Record<string, unknown> | null,
  hints: string[]
): string {
  let md = `# Account State\n\n`;
  md += `**User:** ${user.name ?? user.entityId}\n`;
  if (corp) {
    md += `**Corporation:** ${corp.name}\n`;
    md += `**Credits:** ${corp.credits ?? 0}\n`;
  }
  if (hints.length > 0) {
    md += `\n## Strategy Hints\n`;
    for (const h of hints) {
      md += `- ${h}\n`;
    }
  }
  return md;
}

function formatFleetsState(
  fleetIds: string[],
  hints: string[]
): string {
  let md = `# Fleets (${fleetIds.length})\n\n`;
  if (fleetIds.length > 0) {
    for (const fleetId of fleetIds) {
      md += `- ${fleetId}\n`;
    }
    md += `\nFor detailed fleet status, use \`psecs://fleet/{fleetId}/status\` or \`psecs_fleet_status\`.\n`;
  }
  if (hints.length > 0) {
    md += `\n## Strategy Hints\n`;
    for (const h of hints) {
      md += `- ${h}\n`;
    }
  }
  return md;
}

function formatResearchState(
  data: ResearchStatusResponseModel,
  hints: string[]
): string {
  let md = `# Research State\n\n`;
  md += `**Total Capacity:** ${data.totalCapacity ?? 0}\n`;
  md += `**Allocated:** ${data.totalAllocation ?? 0}\n\n`;
  const allocations = data.activeProjects ?? [];
  if (allocations.length > 0) {
    md += `## Active Projects\n`;
    for (const a of allocations) {
      md += `- ${a.targetId}: ${a.allocationPercent}%\n`;
    }
  }
  if (hints.length > 0) {
    md += `\n## Strategy Hints\n`;
    for (const h of hints) {
      md += `- ${h}\n`;
    }
  }
  return md;
}

function formatManufacturingState(
  data: ManufacturingStatusResponseModel,
  hints: string[]
): string {
  let md = `# Manufacturing State\n\n`;
  const jobs = data.jobs ?? [];
  md += `**Active Jobs:** ${jobs.length}\n\n`;
  if (jobs.length > 0) {
    for (const job of jobs.slice(0, 10)) {
      md += `- **${job.displayName ?? job.jobId}**: ${job.status ?? "Unknown"}\n`;
    }
    if (jobs.length > 10) md += `- ... and ${jobs.length - 10} more\n`;
  }
  if (hints.length > 0) {
    md += `\n## Strategy Hints\n`;
    for (const h of hints) {
      md += `- ${h}\n`;
    }
  }
  return md;
}

function formatMarketState(
  bids: MyBidsItemModel[],
  sales: MarketListingItemModel[],
  hints: string[]
): string {
  let md = `# Market Activity\n\n`;
  md += `**Active Bids:** ${bids.length}\n`;
  md += `**Active Sales:** ${sales.length}\n\n`;
  if (bids.length > 0) {
    md += `## Your Bids\n`;
    for (const bid of bids.slice(0, 10)) {
      md += `- ${bid.assetSummary ?? bid.saleId}: ${bid.yourBidAmount ?? "?"} credits\n`;
    }
  }
  if (sales.length > 0) {
    md += `## Your Sales\n`;
    for (const sale of sales.slice(0, 10)) {
      md += `- ${sale.assetSummary ?? sale.saleId}: ${sale.price ?? "?"} credits (${sale.type ?? "?"})\n`;
    }
  }
  if (hints.length > 0) {
    md += `\n## Strategy Hints\n`;
    for (const h of hints) {
      md += `- ${h}\n`;
    }
  }
  return md;
}

function formatInventoryState(
  data: CorpInventoryResponseModel,
  hints: string[]
): string {
  let md = `# Inventory\n\n`;
  const resources = data.totals ?? [];
  if (resources.length > 0) {
    md += `**Total Resource Types:** ${resources.length}\n\n`;
    for (const res of resources.slice(0, 20)) {
      md += `- ${res.resourceName ?? res.rawResourceId}: ${res.totalQuantity ?? "?"}\n`;
    }
    if (resources.length > 20)
      md += `- ... and ${resources.length - 20} more\n`;
  } else {
    md += `No resources in inventory.\n`;
  }
  if (hints.length > 0) {
    md += `\n## Strategy Hints\n`;
    for (const h of hints) {
      md += `- ${h}\n`;
    }
  }
  return md;
}
