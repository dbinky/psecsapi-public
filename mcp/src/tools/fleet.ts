import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { PsecsClient } from "../client.js";
import { formatToolResult, formatToolError } from "../tool-utils.js";

interface FleetDetails {
  entityId?: string;
  ownerCorpId?: string;
  name?: string;
  sectorId?: string;
  status?: string;
  ships?: string[];
  queueStatus?: {
    conduitId?: string;
    queueWidth?: number;
    queueLength?: number;
    queuePosition?: number;
    enqueuedTimestamp?: string;
    [key: string]: unknown;
  };
  transitETA?: string;
  activeCombatId?: string;
  assignedCombatScriptId?: string;
  [key: string]: unknown;
}

interface ShipDetails {
  entityId?: string;
  name?: string;
  modules?: unknown[];
  cargoHolds?: unknown[];
  [key: string]: unknown;
}

interface ScanResult {
  entityId?: string;
  name?: string;
  type?: string;
  conduits?: Array<{
    entityId: string;
    originSectorId?: string;
    endpointSectorId?: string;
    width?: number;
    length?: number;
    [key: string]: unknown;
  }>;
  orbitals?: Record<string, string>;
  [key: string]: unknown;
}

interface FleetDeepScanResource {
  entityId?: string;
  name?: string;
  type?: string;
  class?: string;
  order?: string;
  propertyAssessments?: Record<string, string>;
  propertyValues?: Record<string, number>;
  [key: string]: unknown;
}

interface SurveyResult {
  fleets?: Array<{
    fleetId: string;
    name?: string;
    shipCount?: number;
    [key: string]: unknown;
  }>;
  [key: string]: unknown;
}

export function registerFleetTools(
  server: McpServer,
  client: PsecsClient
): void {
  server.registerTool(
    "psecs_fleet_status",
    {
      description:
        "Get detailed fleet status including all ship details. " +
        "Returns combined fleet and ship data with strategy suggestions based on fleet state.",
      inputSchema: {
        fleetId: z.string().describe("Fleet ID to inspect"),
      },
    },
    async (args) => {
      const suggestions: string[] = [];
      const warnings: string[] = [];

      // Get fleet details
      const fleetResult = await client.get<FleetDetails>(
        "/api/Fleet/{fleetId}",
        { path: { fleetId: args.fleetId } }
      );
      if (!fleetResult.ok) return formatToolError(fleetResult);
      const fleet = fleetResult.data;

      // Get ship details for each ship in the fleet
      const shipIds = fleet.ships ?? [];
      const shipResults = await Promise.all(
        shipIds.map((shipId) =>
          client.get<ShipDetails>("/api/Ship/{shipId}", {
            path: { shipId },
          })
        )
      );

      const ships: ShipDetails[] = [];
      for (const shipResult of shipResults) {
        if (shipResult.ok) {
          ships.push(shipResult.data);
        } else {
          warnings.push(`Could not fetch details for a ship.`);
        }
      }

      // Generate status-based suggestions
      const status = fleet.status ?? "Unknown";
      if (status === "Idle") {
        suggestions.push(
          "Fleet is idle. Use psecs_explore_sector to scan for resources and conduits."
        );
        suggestions.push(
          "Use psecs_scout_route to check available travel routes."
        );
      } else if (status === "InTransit") {
        suggestions.push(
          "Fleet is in transit. Wait for arrival before issuing new commands."
        );
        if (fleet.transitETA) {
          suggestions.push(`Estimated arrival: ${fleet.transitETA}`);
        }
      } else if (status === "Queued") {
        suggestions.push(
          "Fleet is queued at a conduit. It will enter transit when a lane opens."
        );
        suggestions.push(
          "Use psecs_raw_update_fleet_dequeue to cancel transit if needed."
        );
      } else if (status === "InCombat") {
        warnings.push(
          "Fleet is engaged in combat. Movement and extraction are unavailable until combat resolves."
        );
        if (fleet.activeCombatId) {
          suggestions.push(
            `Active combat ID: ${fleet.activeCombatId}. Combat resolves automatically based on assigned scripts.`
          );
        }
        suggestions.push(
          "Use psecs_raw_update_fleet_combat_script to change the fleet's combat behavior script."
        );
      }

      if (ships.length === 0) {
        warnings.push("Fleet has no ships — it cannot do anything.");
      }

      return formatToolResult({
        fleet,
        ships,
        suggestions,
        warnings,
      });
    }
  );

  server.registerTool(
    "psecs_explore_sector",
    {
      description:
        "Perform a comprehensive exploration of the fleet's current sector. " +
        "Runs a basic scan, deep-scans each orbital for resources, and surveys other fleets. " +
        "Returns the complete sector picture with suggestions about resources and conduits.",
      inputSchema: {
        fleetId: z.string().describe("Fleet ID to explore with"),
      },
    },
    async (args) => {
      const suggestions: string[] = [];
      const warnings: string[] = [];
      const pathOpts = { path: { fleetId: args.fleetId } };

      // Step 1: Basic scan
      const scanResult = await client.get<ScanResult>(
        "/api/Fleet/{fleetId}/scan",
        pathOpts
      );
      if (!scanResult.ok) return formatToolError(scanResult);
      const scan = scanResult.data;

      // Step 2: Deep scan for resources — strategy depends on sector type
      // StarSystem/BlackHole: orbitals is Record<string, string> (e.g. {"0":"Planet","1":"AsteroidBelt"})
      // Nebula/Rubble: no orbital index needed — one sector-wide scan
      const allResources: FleetDeepScanResource[] = [];
      const orbitals = scan.orbitals ?? {};
      const orbitalEntries = Object.entries(orbitals);
      const sectorType = scan.type ?? "";
      const supportsDeepScan = ["StarSystem", "BlackHole", "Nebula", "Rubble"].includes(sectorType);

      if (supportsDeepScan) {
        if (orbitalEntries.length > 0) {
          // StarSystem/BlackHole: scan each orbital by position index
          const deepScanResults = await Promise.all(
            orbitalEntries.map(([orbitalPos]) =>
              client.get<FleetDeepScanResource[]>("/api/Fleet/{fleetId}/scan/deep", {
                path: { fleetId: args.fleetId },
                query: { orbital: parseInt(orbitalPos) },
              })
            )
          );
          for (const deepResult of deepScanResults) {
            if (deepResult.ok) {
              allResources.push(...deepResult.data);
            } else {
              warnings.push("Could not deep-scan an orbital.");
            }
          }
        } else {
          // Nebula/Rubble: sector-wide scan with no orbital parameter
          const deepResult = await client.get<FleetDeepScanResource[]>(
            "/api/Fleet/{fleetId}/scan/deep",
            pathOpts
          );
          if (deepResult.ok) {
            allResources.push(...deepResult.data);
          } else {
            warnings.push("Could not deep-scan sector resources.");
          }
        }
      }

      // Step 3: Survey other fleets
      const surveyResult = await client.get<SurveyResult>(
        "/api/Fleet/{fleetId}/survey",
        pathOpts
      );
      const survey = surveyResult.ok ? surveyResult.data : null;
      if (!surveyResult.ok)
        warnings.push("Could not survey fleets in sector.");

      // Generate suggestions
      const totalResources = allResources.length;
      if (totalResources > 0) {
        suggestions.push(
          `Found ${totalResources} resource(s)${orbitalEntries.length > 0 ? ` across ${orbitalEntries.length} orbital(s)` : ""}. Use psecs_mine_resource to start mining.`
        );
      } else if (supportsDeepScan) {
        suggestions.push(
          "No resources currently available. Resources respawn periodically — check back later."
        );
      }

      const conduits = scan.conduits ?? [];
      if (conduits.length > 0) {
        suggestions.push(
          `${conduits.length} conduit(s) available for travel. Use psecs_navigate to move to another sector.`
        );
      } else {
        warnings.push(
          "No conduits in this sector — fleet may be stranded. " +
          "Use psecs_raw_create_space to generate new sectors and conduits nearby (costs 0.01 tokens per sector, max 100 at a time)."
        );
      }

      const otherFleetCount = Math.max(0, (survey?.fleets ?? []).length - 1);
      if (otherFleetCount > 0) {
        suggestions.push(
          `${otherFleetCount} other fleet(s) detected in sector. Use psecs_assess_threats for threat analysis.`
        );
      }

      // Map & catalog suggestions
      if (totalResources > 0) {
        suggestions.push(
          "Bookmark this sector with psecs_raw_create_usermap_favorite if it has valuable resources. " +
            "Use psecs_raw_update_usermap_note to record resource quality observations."
        );
      }

      return formatToolResult({
        scan,
        resources: allResources,
        survey,
        suggestions,
        warnings,
      });
    }
  );

  server.registerTool(
    "psecs_navigate",
    {
      description:
        "Queue a fleet for transit through a conduit (wormhole) to another sector. " +
        "The fleet must be Idle and in the same sector as the conduit entrance.",
      inputSchema: {
        fleetId: z.string().describe("Fleet ID to move"),
        conduitId: z.string().describe("Conduit ID to travel through"),
      },
    },
    async (args) => {
      const suggestions: string[] = [];
      const warnings: string[] = [];

      const result = await client.put(
        "/api/Fleet/{fleetId}/enqueue",
        { conduitId: args.conduitId },
        { path: { fleetId: args.fleetId } }
      );
      if (!result.ok) return formatToolError(result);

      suggestions.push(
        "Fleet queued for transit. Use psecs_fleet_status to monitor transit progress."
      );
      suggestions.push(
        "After arrival, use psecs_explore_sector to scan the new sector."
      );

      return formatToolResult({
        ...result.data as Record<string, unknown>,
        suggestions,
        warnings,
      });
    }
  );

  server.registerTool(
    "psecs_scout_route",
    {
      description:
        "Scan the fleet's current sector and report available conduits (travel routes). " +
        "Use this to plan your next move before navigating.",
      inputSchema: {
        fleetId: z.string().describe("Fleet ID to scout with"),
      },
    },
    async (args) => {
      const suggestions: string[] = [];
      const warnings: string[] = [];

      const scanResult = await client.get<ScanResult>(
        "/api/Fleet/{fleetId}/scan",
        { path: { fleetId: args.fleetId } }
      );
      if (!scanResult.ok) return formatToolError(scanResult);
      const scan = scanResult.data;

      const conduits = scan.conduits ?? [];
      if (conduits.length === 0) {
        warnings.push(
          "No conduits found in this sector. The fleet cannot travel anywhere from here."
        );
      } else {
        suggestions.push(
          `${conduits.length} conduit(s) available. Use psecs_navigate with a conduitId to travel.`
        );
      }

      return formatToolResult({
        sectorId: scan.entityId,
        sectorName: scan.name,
        sectorType: scan.type,
        conduits,
        suggestions,
        warnings,
      });
    }
  );

  server.registerTool(
    "psecs_assess_threats",
    {
      description:
        "Survey all fleets visible in the fleet's current sector and assess threat level. " +
        "Returns information about other fleets present and safety suggestions.",
      inputSchema: {
        fleetId: z.string().describe("Fleet ID performing the survey"),
      },
    },
    async (args) => {
      const suggestions: string[] = [];
      const warnings: string[] = [];

      const surveyResult = await client.get<SurveyResult>(
        "/api/Fleet/{fleetId}/survey",
        { path: { fleetId: args.fleetId } }
      );
      if (!surveyResult.ok) return formatToolError(surveyResult);
      const survey = surveyResult.data;

      const fleets = survey.fleets ?? [];
      // The survey includes our own fleet, so other fleets = total - 1
      const otherFleetCount = Math.max(0, fleets.length - 1);

      if (otherFleetCount === 0) {
        suggestions.push("Sector appears clear — no other fleets detected.");
      } else if (otherFleetCount <= 2) {
        suggestions.push(
          `${otherFleetCount} other fleet(s) in sector. Moderate caution advised.`
        );
        suggestions.push(
          "Use psecs_raw_fleet_scan_fleet to inspect specific fleets for more detail."
        );
      } else {
        warnings.push(
          `${otherFleetCount} other fleet(s) in sector — high traffic area.`
        );
        suggestions.push(
          "Consider navigating to a quieter sector if you want to mine safely."
        );
        suggestions.push(
          "Use psecs_raw_fleet_scan_fleet to inspect specific fleets."
        );
      }

      return formatToolResult({
        survey,
        otherFleetCount,
        suggestions,
        warnings,
      });
    }
  );
}
