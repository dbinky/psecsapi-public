import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { PsecsClient } from "../client.js";
import { formatToolResult, formatToolError } from "../tool-utils.js";

interface ExtractionFleetInfo {
  entityId?: string;
  name?: string;
  sectorId?: string;
  status?: string;
  ships?: string[];
  [key: string]: unknown;
}

interface ExtractionJobStatus {
  jobId?: string;
  rawResourceId?: string;
  resourceName?: string;
  ratePerMinute?: number;
  accumulatedQuantity?: number;
  quantityLimit?: number;
  startTime?: string;
  [key: string]: unknown;
}

interface ExtractionStartResult {
  jobId?: string;
  rawResourceId?: string;
  resourceName?: string;
  ratePerMinute?: number;
  accumulatedQuantity?: number;
  quantityLimit?: number;
  startTime?: string;
  [key: string]: unknown;
}

interface DeepScanResource {
  entityId?: string;
  name?: string;
  type?: string;
  class?: string;
  order?: string;
  propertyAssessments?: Record<string, string>;
  propertyValues?: Record<string, number>;
  [key: string]: unknown;
}

interface ExtractionModifiers {
  modifiers?: Record<string, number>;
  [key: string]: unknown;
}

interface MaterializationResult {
  jobId?: string;
  boxedResourceId?: string;
  rawResourceId?: string;
  resourceName?: string;
  materializedQuantity?: number;
  [key: string]: unknown;
}

export function registerExtractionTools(
  server: McpServer,
  client: PsecsClient
): void {
  server.registerTool(
    "psecs_mine_resource",
    {
      description:
        "Start mining a resource with a ship. The ship must have extraction modules " +
        "installed and be in a sector where the resource is available. " +
        "Returns extraction job status and next-step suggestions.",
      inputSchema: {
        shipId: z.string().describe("Ship ID to perform extraction with"),
        resourceId: z.string().describe("Resource ID to extract (from deep scan results)"),
        quantityLimit: z
          .number()
          .optional()
          .describe("Optional limit on quantity to extract before auto-stopping"),
      },
    },
    async (args) => {
      const suggestions: string[] = [];
      const warnings: string[] = [];

      const body: Record<string, unknown> = { resourceId: args.resourceId };
      if (args.quantityLimit !== undefined) {
        body.quantityLimit = args.quantityLimit;
      }

      const result = await client.post<ExtractionStartResult>(
        "/api/Ship/{shipId}/extraction",
        body,
        { path: { shipId: args.shipId } }
      );
      if (!result.ok) return formatToolError(result);

      suggestions.push(
        "Extraction started. Use psecs_extraction_status to monitor progress across all ships."
      );
      suggestions.push(
        "Use psecs_stop_extraction to stop mining and collect resources into cargo."
      );
      if (!args.quantityLimit) {
        suggestions.push(
          "No quantity limit set — extraction will continue until manually stopped or cargo is full."
        );
      }

      return formatToolResult({
        extraction: result.data,
        suggestions,
        warnings,
      });
    }
  );

  server.registerTool(
    "psecs_extraction_status",
    {
      description:
        "Get extraction status for all ships in a fleet. " +
        "Shows which ships are actively mining, what they're extracting, and quantities. " +
        "Returns combined view with suggestions for idle ships.",
      inputSchema: {
        fleetId: z.string().describe("Fleet ID to check extraction status for"),
      },
    },
    async (args) => {
      const suggestions: string[] = [];
      const warnings: string[] = [];

      // Step 1: Get fleet details to find ship list
      const fleetResult = await client.get<ExtractionFleetInfo>(
        "/api/Fleet/{fleetId}",
        { path: { fleetId: args.fleetId } }
      );
      if (!fleetResult.ok) return formatToolError(fleetResult);
      const fleet = fleetResult.data;

      const shipIds = fleet.ships ?? [];
      if (shipIds.length === 0) {
        warnings.push("Fleet has no ships.");
        return formatToolResult({ fleet, shipExtractions: [], suggestions, warnings });
      }

      // Step 2: Get extraction status for each ship in parallel
      const extractionResults = await Promise.all(
        shipIds.map((shipId) =>
          client.get<ExtractionJobStatus[]>("/api/Ship/{shipId}/extraction", {
            path: { shipId },
          })
        )
      );

      const shipExtractions: Array<{
        shipId: string;
        extractions: ExtractionJobStatus[];
      }> = [];
      let activeCount = 0;
      let idleCount = 0;

      for (let i = 0; i < shipIds.length; i++) {
        const result = extractionResults[i];
        if (result.ok) {
          const extractions = result.data;
          shipExtractions.push({ shipId: shipIds[i], extractions });
          if (extractions.length > 0) {
            activeCount++;
          } else {
            idleCount++;
          }
        } else {
          warnings.push(`Could not fetch extraction status for ship ${shipIds[i]}.`);
        }
      }

      // Generate suggestions
      if (idleCount > 0) {
        suggestions.push(
          `${idleCount} ship(s) have no active extractions. Use psecs_explore_sector to find resources, then psecs_mine_resource to start mining.`
        );
      }
      if (activeCount > 0) {
        suggestions.push(
          `${activeCount} ship(s) are actively extracting. Use psecs_stop_extraction to stop and collect resources when ready.`
        );
      }
      if (activeCount === 0 && idleCount > 0) {
        suggestions.push(
          "No ships are currently mining. Use psecs_optimize_extraction to find the best resources to extract."
        );
      }

      return formatToolResult({
        fleet: { entityId: fleet.entityId, name: fleet.name, status: fleet.status },
        shipExtractions,
        summary: { totalShips: shipIds.length, activeExtractions: activeCount, idleShips: idleCount },
        suggestions,
        warnings,
      });
    }
  );

  server.registerTool(
    "psecs_optimize_extraction",
    {
      description:
        "Analyze available resources and research modifiers to suggest optimal extraction targets. " +
        "Deep-scans the fleet's sector and cross-references with active research modifiers " +
        "to recommend the best resources to mine.",
      inputSchema: {
        fleetId: z.string().describe("Fleet ID to optimize extraction for"),
        orbital: z
          .number()
          .optional()
          .describe("Specific orbital index to scan (scans all if not provided)"),
      },
    },
    async (args) => {
      const suggestions: string[] = [];
      const warnings: string[] = [];

      // Determine scan strategy based on sector type.
      // StarSystem and BlackHole require per-orbital scans; Nebula/Rubble are sector-wide.
      // If the caller specified an orbital, use it directly without the preliminary scan.
      let resources: DeepScanResource[] = [];
      let modifiersResult: Awaited<ReturnType<typeof client.get<ExtractionModifiers>>>;

      if (args.orbital !== undefined) {
        // Caller specified an orbital — scan it directly in parallel with modifiers
        const [deepScanResult, modRes] = await Promise.all([
          client.get<DeepScanResource[]>("/api/Fleet/{fleetId}/scan/deep", {
            path: { fleetId: args.fleetId },
            query: { orbital: args.orbital },
          }),
          client.get<ExtractionModifiers>("/api/Research/modifiers"),
        ]);
        modifiersResult = modRes;
        if (!deepScanResult.ok) return formatToolError(deepScanResult);
        resources = deepScanResult.data;
      } else {
        // No orbital specified — do a basic scan first to determine sector type and orbitals
        const [basicScanResult, modRes] = await Promise.all([
          client.get<{ type?: string; orbitals?: Record<string, string> }>("/api/Fleet/{fleetId}/scan", {
            path: { fleetId: args.fleetId },
          }),
          client.get<ExtractionModifiers>("/api/Research/modifiers"),
        ]);
        modifiersResult = modRes;
        if (!basicScanResult.ok) return formatToolError(basicScanResult);

        const sectorType = basicScanResult.data.type ?? "";
        const orbitalEntries = Object.entries(basicScanResult.data.orbitals ?? {});
        const needsPerOrbitalScan = ["StarSystem", "BlackHole"].includes(sectorType);

        if (needsPerOrbitalScan && orbitalEntries.length > 0) {
          // Scan each orbital in parallel
          const deepScanResults = await Promise.all(
            orbitalEntries.map(([orbitalPos]) =>
              client.get<DeepScanResource[]>("/api/Fleet/{fleetId}/scan/deep", {
                path: { fleetId: args.fleetId },
                query: { orbital: parseInt(orbitalPos) },
              })
            )
          );
          for (const r of deepScanResults) {
            if (r.ok) {
              resources.push(...r.data);
            } else {
              warnings.push("Could not deep-scan an orbital.");
            }
          }
        } else {
          // Nebula/Rubble/Void: sector-wide scan
          const deepScanResult = await client.get<DeepScanResource[]>("/api/Fleet/{fleetId}/scan/deep", {
            path: { fleetId: args.fleetId },
          });
          if (!deepScanResult.ok) return formatToolError(deepScanResult);
          resources = deepScanResult.data;
        }
      }

      const modifiers = modifiersResult.ok ? modifiersResult.data : null;
      if (!modifiersResult.ok)
        warnings.push("Could not fetch research modifiers — optimization suggestions may be incomplete.");

      if (resources.length === 0) {
        suggestions.push(
          "No resources currently available in this sector. Resources respawn over time (96-192 hours) — check back later or navigate to another sector."
        );
      } else {
        suggestions.push(
          `Found ${resources.length} resource(s) available for extraction.`
        );

        // Check for extraction-related modifiers
        const modifierMap = modifiers?.modifiers ?? {};
        const extractionModifiers = Object.entries(modifierMap).filter(
          ([key]) => key.toLowerCase().includes("extraction")
        );

        if (extractionModifiers.length > 0) {
          const modifierList = extractionModifiers
            .map(([key, value]) => `${key}: +${value}%`)
            .join(", ");
          suggestions.push(
            `Active extraction modifiers: ${modifierList}. Prioritize resources matching your bonuses.`
          );
        } else {
          suggestions.push(
            "No extraction modifiers active. Research extraction-boosting technologies to increase yield."
          );
        }

        // Suggest highest overall quality resources (using OQ property value)
        const sortedByQuality = [...resources]
          .filter((r) => r.propertyValues?.["OQ"] !== undefined)
          .sort((a, b) => (b.propertyValues?.["OQ"] ?? 0) - (a.propertyValues?.["OQ"] ?? 0));
        if (sortedByQuality.length > 0) {
          const best = sortedByQuality[0];
          const oq = best.propertyValues?.["OQ"];
          suggestions.push(
            `Highest quality resource: ${best.name ?? best.entityId ?? "Unknown"} (OQ: ${oq}). Use psecs_mine_resource with resourceId "${best.entityId}" to start extraction.`
          );
        }
      }

      return formatToolResult({
        resources,
        modifiers,
        resourceCount: resources.length,
        suggestions,
        warnings,
      });
    }
  );

  server.registerTool(
    "psecs_stop_extraction",
    {
      description:
        "Stop extraction on a ship and collect the mined resources into cargo. " +
        "Ships can run multiple extraction jobs concurrently (one per extraction module), " +
        "so a single ship may have several active jobs at once. " +
        "By default stops the first active job; use jobId to target a specific job, " +
        "or set stopAll to true to stop every extraction on the ship at once. " +
        "When stopping a single job, returns a single MaterializationResult object " +
        "(with jobId, resourceName, materializedQuantity, boxedResourceId). " +
        "When stopAll is true, returns an array of MaterializationResult objects, one per stopped job.",
      inputSchema: {
        shipId: z.string().describe("Ship ID to stop extraction on"),
        jobId: z
          .string()
          .optional()
          .describe("Specific extraction job ID to stop (optional — stops first active job if omitted)"),
        stopAll: z
          .boolean()
          .optional()
          .describe("Stop all active extractions on this ship (default false)"),
      },
    },
    async (args) => {
      const suggestions: string[] = [];
      const warnings: string[] = [];
      const pathOpts = { path: { shipId: args.shipId } };

      if (args.stopAll) {
        // Stop all extractions on the ship
        const result = await client.delete<MaterializationResult[]>(
          "/api/Ship/{shipId}/extraction/all",
          pathOpts
        );
        if (!result.ok) return formatToolError(result);

        const results = result.data;
        if (results.length === 0) {
          warnings.push("No active extractions found on this ship.");
        } else {
          const totalQty = results.reduce(
            (sum, r) => sum + (r.materializedQuantity ?? 0),
            0
          );
          suggestions.push(
            `Stopped ${results.length} extraction(s). Collected ${totalQty} total units across ${results.length} resource(s).`
          );
          for (const r of results) {
            suggestions.push(
              `  - ${r.resourceName ?? r.rawResourceId ?? "Unknown"}: ${r.materializedQuantity ?? 0} units → cargo (asset ${r.boxedResourceId})`
            );
          }
        }

        suggestions.push(
          "Resources are now in the ship's cargo. Use psecs_ship_cargo_overview to see contents."
        );
        suggestions.push(
          "To see total corp-wide resource holdings across all ships, use psecs_raw_corp_inventory."
        );
        suggestions.push(
          "Use psecs_market_sell to list resources for sale, or use them for manufacturing with psecs_start_manufacturing."
        );

        return formatToolResult({
          action: "stopAll",
          results,
          suggestions,
          warnings,
        });
      } else {
        // Stop a single extraction (specific job or first active)
        const queryOpts: { path: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {
          ...pathOpts,
        };
        if (args.jobId) {
          queryOpts.query = { jobId: args.jobId };
        }

        const result = await client.delete<MaterializationResult>(
          "/api/Ship/{shipId}/extraction",
          queryOpts
        );
        if (!result.ok) return formatToolError(result);

        const mat = result.data;
        if (mat.materializedQuantity !== undefined && mat.materializedQuantity > 0) {
          suggestions.push(
            `Collected ${mat.materializedQuantity} units of ${mat.resourceName ?? "resource"} → cargo (asset ${mat.boxedResourceId}).`
          );
        } else {
          suggestions.push("Extraction stopped. No resources were collected (job may have just started).");
        }

        suggestions.push(
          "Resources are now in the ship's cargo. Use psecs_ship_cargo_overview to see contents."
        );
        suggestions.push(
          "To see total corp-wide resource holdings across all ships, use psecs_raw_corp_inventory."
        );
        suggestions.push(
          "Use psecs_market_sell to list resources for sale, or use them for manufacturing with psecs_start_manufacturing."
        );

        return formatToolResult({
          action: "stop",
          result: mat,
          suggestions,
          warnings,
        });
      }
    }
  );
}
