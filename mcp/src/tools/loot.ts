import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { PsecsClient } from "../client.js";
import { formatToolResult, formatToolError } from "../tool-utils.js";

interface FleetInfo {
  entityId: string;
  sectorId?: string;
  status?: string;
  ships?: string[];
  [key: string]: unknown;
}

interface LootField {
  id: string;
  positionX?: number;
  positionY?: number;
  itemCount?: number;
  isExclusive?: boolean;
  expiresAt?: string;
  [key: string]: unknown;
}

interface PickupResult {
  [key: string]: unknown;
}

export function registerLootTools(
  server: McpServer,
  client: PsecsClient
): void {
  server.registerTool(
    "psecs_scan_loot",
    {
      description:
        "Scan the fleet's current sector for loot fields dropped by destroyed ships. " +
        "Loot fields appear after combat — the victor has exclusive pickup for 1 hour, " +
        "then they become public. All loot despawns after 24 hours. " +
        "Use this after winning combat or when entering a sector to check for unclaimed loot.",
      inputSchema: {
        fleetId: z
          .string()
          .describe("Fleet ID to scan with (determines which sector to check)"),
      },
    },
    async (args) => {
      const suggestions: string[] = [];
      const warnings: string[] = [];

      // Step 1: Get fleet details to find sectorId
      const fleetResult = await client.get<FleetInfo>(
        "/api/Fleet/{fleetId}",
        { path: { fleetId: args.fleetId } }
      );
      if (!fleetResult.ok) return formatToolError(fleetResult);
      const fleet = fleetResult.data;

      if (!fleet.sectorId) {
        warnings.push("Fleet has no sector — it may be in transit.");
        return formatToolResult({ fleet, lootFields: [], suggestions, warnings });
      }

      // Step 2: Scan sector for loot fields
      const lootResult = await client.get<LootField[]>(
        "/api/sector/{sectorId}/loot",
        { path: { sectorId: fleet.sectorId } }
      );
      if (!lootResult.ok) return formatToolError(lootResult);

      const lootFields = lootResult.data;

      if (lootFields.length === 0) {
        suggestions.push(
          "No loot fields in this sector. Loot appears when ships are destroyed in combat."
        );
        return formatToolResult({
          sectorId: fleet.sectorId,
          lootFields: [],
          suggestions,
          warnings,
        });
      }

      // Analyze loot fields
      const exclusiveFields = lootFields.filter((f) => f.isExclusive);
      const publicFields = lootFields.filter((f) => !f.isExclusive);
      const totalItems = lootFields.reduce(
        (sum, f) => sum + (f.itemCount ?? 0),
        0
      );

      suggestions.push(
        `Found ${lootFields.length} loot field(s) with ${totalItems} total item(s).`
      );

      if (exclusiveFields.length > 0) {
        suggestions.push(
          `${exclusiveFields.length} field(s) are exclusive to the victor — only the winning corp can pick these up.`
        );
      }

      if (publicFields.length > 0) {
        suggestions.push(
          `${publicFields.length} field(s) are public — any fleet in the sector can pick these up. Act fast!`
        );
      }

      // Check expiry times and warn about soon-to-expire fields
      const now = new Date();
      for (const field of lootFields) {
        if (field.expiresAt) {
          const expiry = new Date(field.expiresAt);
          const hoursRemaining = (expiry.getTime() - now.getTime()) / (1000 * 60 * 60);
          if (hoursRemaining < 1) {
            warnings.push(
              `Loot field ${field.id} expires in less than 1 hour — collect it now!`
            );
          }
        }
      }

      // Suggest pickup
      const shipIds = fleet.ships ?? [];
      if (shipIds.length > 0) {
        suggestions.push(
          "Use psecs_pickup_loot with a lootId and shipId to collect items into a ship's cargo."
        );
      } else {
        warnings.push(
          "Fleet has no ships — you cannot pick up loot without a ship to receive the cargo."
        );
      }

      return formatToolResult({
        sectorId: fleet.sectorId,
        lootFields,
        summary: {
          totalFields: lootFields.length,
          exclusiveFields: exclusiveFields.length,
          publicFields: publicFields.length,
          totalItems,
        },
        suggestions,
        warnings,
      });
    }
  );

  server.registerTool(
    "psecs_pickup_loot",
    {
      description:
        "Pick up items from a loot field into a ship's cargo hold. " +
        "The fleet must be in the same sector as the loot field. " +
        "During the first hour after combat, only the victor's corp can pick up. " +
        "After that, any fleet in the sector can collect. Loot despawns after 24 hours.",
      inputSchema: {
        fleetId: z
          .string()
          .describe("Fleet ID in the same sector as the loot field"),
        lootId: z
          .string()
          .describe("Loot field ID to pick up from (from psecs_scan_loot results)"),
        shipId: z
          .string()
          .describe("Ship ID within the fleet that will receive the cargo"),
      },
    },
    async (args) => {
      const suggestions: string[] = [];
      const warnings: string[] = [];

      // Step 1: Get fleet details to find sectorId
      const fleetResult = await client.get<FleetInfo>(
        "/api/Fleet/{fleetId}",
        { path: { fleetId: args.fleetId } }
      );
      if (!fleetResult.ok) return formatToolError(fleetResult);
      const fleet = fleetResult.data;

      if (!fleet.sectorId) {
        warnings.push("Fleet has no sector — it may be in transit.");
        return formatToolResult({ suggestions, warnings });
      }

      // Step 2: Pick up the loot
      const pickupResult = await client.post<PickupResult>(
        "/api/sector/{sectorId}/loot/{lootId}/pickup",
        { fleetId: args.fleetId, shipId: args.shipId },
        {
          path: {
            sectorId: fleet.sectorId,
            lootId: args.lootId,
          },
        }
      );
      if (!pickupResult.ok) return formatToolError(pickupResult);

      const items = pickupResult.data;

      suggestions.push(
        "Loot collected! Items have been added to the ship's cargo hold."
      );
      suggestions.push(
        `Use psecs_ship_cargo_overview with shipId "${args.shipId}" to see what you picked up.`
      );
      suggestions.push(
        "Use psecs_scan_loot to check for more loot fields in the sector."
      );
      suggestions.push(
        "Valuable loot can be sold on the market with psecs_market_sell or used in manufacturing."
      );

      return formatToolResult({
        sectorId: fleet.sectorId,
        lootId: args.lootId,
        collectedItems: items,
        suggestions,
        warnings,
      });
    }
  );
}
