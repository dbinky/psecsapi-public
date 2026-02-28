import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { PsecsClient } from "../client.js";
import { formatToolResult, formatToolError } from "../tool-utils.js";

interface ShipCatalogEntry {
  catalogId: string;
  name?: string;
  class?: string;
  interiorSlots?: number;
  exteriorSlots?: number;
  totalSlots?: number;
  baseStructurePoints?: number;
  baseHullPoints?: number;
  baseMass?: number;
  [key: string]: unknown;
}

interface ShipyardQueueEntry {
  orderNumber?: number;
  totalSlots?: number;
  progressPercent?: number;
  estimatedMinutesRemaining?: number;
  isOwnOrder?: boolean;
  placedTimestamp?: string;
  [key: string]: unknown;
}

interface ShipyardQueueResponse {
  currentBuild?: ShipyardQueueEntry;
  queuedBuilds?: ShipyardQueueEntry[];
  totalQueueDepth?: number;
  [key: string]: unknown;
}

interface CompletedOrderEntry {
  orderNumber?: number;
  chassisName?: string;
  catalogId?: string;
  blueprintQuality?: number;
  interiorSlots?: number;
  exteriorSlots?: number;
  totalSlots?: number;
  completedTimestamp?: string;
  [key: string]: unknown;
}

interface CompletedOrdersResponse {
  orders?: CompletedOrderEntry[];
  total?: number;
  [key: string]: unknown;
}

interface ChassisBlueprintDetail {
  blueprintId?: string;
  chassisClass?: string;
  baseWorkUnitsPerSlot?: number;
  baseInputResources?: Array<{ label?: string; qualifier?: string; value?: string; quantity?: number; [key: string]: unknown }>;
  baseInputComponents?: Array<{ label?: string; componentType?: string; quantity?: number; [key: string]: unknown }>;
  perInteriorSlotInputResources?: Array<{ label?: string; qualifier?: string; value?: string; quantity?: number; [key: string]: unknown }>;
  perInteriorSlotInputComponents?: Array<{ label?: string; componentType?: string; quantity?: number; [key: string]: unknown }>;
  perExteriorSlotInputResources?: Array<{ label?: string; qualifier?: string; value?: string; quantity?: number; [key: string]: unknown }>;
  perExteriorSlotInputComponents?: Array<{ label?: string; componentType?: string; quantity?: number; [key: string]: unknown }>;
  calculatedTotalResources?: Array<{ label?: string; qualifier?: string; value?: string; quantity?: number; [key: string]: unknown }>;
  calculatedTotalComponents?: Array<{ label?: string; componentType?: string; quantity?: number; [key: string]: unknown }>;
  calculatedTotalWorkUnits?: number;
  [key: string]: unknown;
}

interface BuildOrderResult {
  success?: boolean;
  errorMessage?: string;
  orderNumber?: number;
  totalWorkUnits?: number;
  queuePosition?: number;
  estimatedMinutes?: number;
  buildFee?: number;
  [key: string]: unknown;
}

interface PickupResult {
  success?: boolean;
  errorMessage?: string;
  shipId?: string;
  shipDetail?: Record<string, unknown>;
  [key: string]: unknown;
}

export function registerShipyardTools(
  server: McpServer,
  client: PsecsClient
): void {
  server.registerTool(
    "psecs_shipyard_browse",
    {
      description:
        "Browse available ship classes from the shipyard catalog and check the current build queue. " +
        "Shows what ships you can build and what's currently being built. " +
        "Use this to plan fleet expansion before starting a build.",
      inputSchema: {
        shipClass: z
          .string()
          .optional()
          .describe("Optional ship class filter (e.g., Scout, Corvette, Frigate)"),
      },
    },
    async (args) => {
      const suggestions: string[] = [];
      const warnings: string[] = [];

      // Parallel fetch: ship catalog, build queue, and completed orders
      const catalogQuery: { query?: Record<string, string | number | boolean | undefined> } = {};
      if (args.shipClass) {
        catalogQuery.query = { class: args.shipClass };
      }

      const [catalogResult, queueResult, completedResult] = await Promise.all([
        client.get<ShipCatalogEntry[]>("/api/ship-catalog", catalogQuery),
        client.get<ShipyardQueueResponse>("/api/shipyard/queue"),
        client.get<CompletedOrdersResponse>("/api/shipyard/completed"),
      ]);

      if (!catalogResult.ok) return formatToolError(catalogResult);
      const catalog = catalogResult.data;

      const queue = queueResult.ok ? queueResult.data : null;
      if (!queueResult.ok) warnings.push("Could not fetch build queue.");

      const completedOrders = completedResult.ok ? completedResult.data : null;
      if (!completedResult.ok) warnings.push("Could not fetch completed orders.");

      // Catalog suggestions
      if (catalog.length === 0) {
        suggestions.push(
          args.shipClass
            ? `No ship configurations found for class "${args.shipClass}". Try without a class filter to see all options.`
            : "No ship configurations found in the catalog."
        );
      } else {
        const classCounts = new Map<string, number>();
        for (const entry of catalog) {
          const cls = entry.class ?? "Unknown";
          classCounts.set(cls, (classCounts.get(cls) ?? 0) + 1);
        }
        const classSum = [...classCounts.entries()]
          .map(([cls, count]) => `${cls}: ${count}`)
          .join(", ");
        suggestions.push(
          `${catalog.length} ship configuration(s) available (${classSum}).`
        );
        suggestions.push(
          "To build a ship, you need a chassis blueprint instance. " +
            "Check psecs_manufacturing_overview to see if you own any chassis blueprints, " +
            "or psecs_research_overview to find chassis blueprint technologies to research."
        );
        suggestions.push(
          "Use psecs_raw_shipyard_blueprint with a blueprint ID and slot counts to see detailed input costs before building."
        );
        suggestions.push(
          "Once you have a catalog ID and blueprint instance ID, use psecs_shipyard_start_build to place a build order."
        );
      }

      // Queue suggestions
      if (queue) {
        const depth = queue.totalQueueDepth ?? 0;
        if (depth === 0) {
          suggestions.push(
            "Shipyard queue is empty — builds will start immediately."
          );
        } else {
          suggestions.push(
            `Shipyard queue depth: ${depth}. ` +
              (queue.currentBuild
                ? `Current build at ${queue.currentBuild.progressPercent ?? 0}% (est. ${queue.currentBuild.estimatedMinutesRemaining ?? "?"} min remaining).`
                : "No build currently in progress.")
          );
        }

        const ownBuilds = [
          ...(queue.currentBuild?.isOwnOrder ? [queue.currentBuild] : []),
          ...(queue.queuedBuilds?.filter((b) => b.isOwnOrder) ?? []),
        ];
        if (ownBuilds.length > 0) {
          for (const build of ownBuilds) {
            const progress =
              build.progressPercent !== undefined && build.progressPercent > 0
                ? ` (${build.progressPercent}% complete)`
                : "";
            suggestions.push(
              `Your build order #${build.orderNumber}${progress} — ` +
                (build.progressPercent !== undefined && build.progressPercent >= 100
                  ? "READY FOR PICKUP! Use psecs_shipyard_pickup to collect it."
                  : `est. ${build.estimatedMinutesRemaining ?? "?"} min remaining.`)
            );
          }
        }
      }

      // Completed orders ready for pickup
      const readyOrders = completedOrders?.orders ?? [];
      if (readyOrders.length > 0) {
        suggestions.push(
          `${readyOrders.length} ship(s) ready for pickup: ` +
            readyOrders
              .map((o) => `Order #${o.orderNumber} — ${o.chassisName ?? o.catalogId ?? "unknown"}`)
              .join(", ") +
            ". Use psecs_shipyard_pickup with the order number and a fleet ID at Nexus."
        );
      }

      return formatToolResult({
        catalog,
        queue,
        completedOrders,
        suggestions,
        warnings,
      });
    }
  );

  server.registerTool(
    "psecs_shipyard_start_build",
    {
      description:
        "Start building a new ship at the Nexus Station shipyard. " +
        "Fetches blueprint cost details before submitting the build order. " +
        "You need a ship catalog ID (from psecs_shipyard_browse), a chassis blueprint instance ID " +
        "(from psecs_manufacturing_overview or research), and selected input assets. " +
        "Input assets can come from ship cargo OR directly from the Nexus Warehouse — " +
        "you do NOT need to move warehouse assets to ship cargo first, just submit their asset IDs directly. " +
        "If using ship cargo assets, the ship must be in a fleet that is idle at a Nexus sector.",
      inputSchema: {
        catalogId: z
          .string()
          .describe("Ship catalog ID (from psecs_shipyard_browse, e.g., 'scout-basic')"),
        blueprintInstanceId: z
          .string()
          .describe("Chassis blueprint instance ID (GUID — from your owned blueprints)"),
        selectedInputs: z
          .record(z.array(z.string()))
          .describe(
            "Map of input label to list of boxed asset IDs. " +
              "Keys are input labels from the blueprint (e.g., 'Hull Plating'), " +
              "values are arrays of boxed asset IDs to use for that input. " +
              "Asset IDs can come from ship cargo OR the Nexus Warehouse — both are accepted."
          ),
        interiorSlots: z
          .number()
          .optional()
          .describe("Number of interior module slots to build (for cost preview)"),
        exteriorSlots: z
          .number()
          .optional()
          .describe("Number of exterior module slots to build (for cost preview)"),
      },
    },
    async (args) => {
      const suggestions: string[] = [];
      const warnings: string[] = [];

      // Step 1: Fetch blueprint details for context (non-blocking — proceed with build even if this fails)
      // Catalog IDs like "scout-1" map to blueprint IDs like "scout-chassis"
      const chassisClass = args.catalogId.split("-")[0];
      const blueprintId = `${chassisClass}-chassis`;
      const blueprintQuery: { path: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {
        path: { blueprintId },
      };
      if (args.interiorSlots !== undefined || args.exteriorSlots !== undefined) {
        blueprintQuery.query = {
          interiorSlots: args.interiorSlots,
          exteriorSlots: args.exteriorSlots,
        };
      }

      const blueprintResult = await client.get<ChassisBlueprintDetail>(
        "/api/shipyard/blueprint/{blueprintId}",
        blueprintQuery
      );
      const blueprint = blueprintResult.ok ? blueprintResult.data : null;
      if (!blueprintResult.ok) {
        warnings.push(
          "Could not fetch blueprint details — proceeding with build attempt."
        );
      }

      // Show blueprint cost context
      if (blueprint) {
        if (blueprint.calculatedTotalResources && blueprint.calculatedTotalResources.length > 0) {
          const totalCosts = [
            ...blueprint.calculatedTotalResources.map(
              (r) => `${r.quantity ?? "?"} x ${r.label ?? r.value ?? "resource"}`
            ),
            ...(blueprint.calculatedTotalComponents ?? []).map(
              (c) => `${c.quantity ?? "?"} x ${c.label ?? c.componentType ?? "component"}`
            ),
          ].join(", ");
          suggestions.push(`Blueprint total costs: ${totalCosts}`);
        } else {
          // Show base costs if calculated totals unavailable
          const baseCosts = [
            ...(blueprint.baseInputResources ?? []).map(
              (r) => `${r.quantity ?? "?"} x ${r.label ?? "resource"} (base)`
            ),
            ...(blueprint.baseInputComponents ?? []).map(
              (c) => `${c.quantity ?? "?"} x ${c.label ?? "component"} (base)`
            ),
          ];
          if (baseCosts.length > 0) {
            suggestions.push(
              `Blueprint base costs (per slot costs additional): ${baseCosts.join(", ")}. ` +
                "Provide interiorSlots and exteriorSlots for calculated totals."
            );
          }
        }
      }

      // Step 2: Submit the build order
      const body = {
        catalogId: args.catalogId,
        blueprintInstanceId: args.blueprintInstanceId,
        selectedInputs: args.selectedInputs,
      };

      const result = await client.post<BuildOrderResult>(
        "/api/shipyard/build",
        body
      );
      if (!result.ok) return formatToolError(result);

      const buildOrder = result.data;
      if (buildOrder.success === false) {
        warnings.push(buildOrder.errorMessage ?? "Build order failed.");
        return formatToolResult({
          result: buildOrder,
          blueprintDetails: blueprint,
          suggestions,
          warnings,
        });
      }

      suggestions.push(
        `Build order placed! Order #${buildOrder.orderNumber}, queue position ${buildOrder.queuePosition ?? "?"}.`
      );
      if (buildOrder.estimatedMinutes !== undefined) {
        suggestions.push(
          `Estimated build time: ${buildOrder.estimatedMinutes} minutes.`
        );
      }
      if (buildOrder.buildFee !== undefined && buildOrder.buildFee > 0) {
        suggestions.push(
          `Build fee charged: ${buildOrder.buildFee} credits.`
        );
      }
      suggestions.push(
        "Use psecs_shipyard_browse to monitor the build queue progress."
      );
      suggestions.push(
        "When the build completes, use psecs_shipyard_pickup to collect your new ship into a fleet."
      );

      return formatToolResult({
        result: buildOrder,
        blueprintDetails: blueprint,
        suggestions,
        warnings,
      });
    }
  );

  server.registerTool(
    "psecs_shipyard_pickup",
    {
      description:
        "Pick up a completed ship from the shipyard and add it to a fleet. " +
        "The build order must be complete (100% progress). " +
        "The fleet must be located at a Nexus sector.",
      inputSchema: {
        orderNumber: z
          .number()
          .describe("Build order number (from psecs_shipyard_browse queue)"),
        fleetId: z
          .string()
          .describe("Fleet ID to add the new ship to (must be at a Nexus sector)"),
      },
    },
    async (args) => {
      const suggestions: string[] = [];
      const warnings: string[] = [];

      const result = await client.post<PickupResult>(
        "/api/shipyard/pickup/{orderNumber}",
        undefined,
        {
          path: { orderNumber: String(args.orderNumber) },
          query: { fleetId: args.fleetId },
        }
      );
      if (!result.ok) return formatToolError(result);

      const pickup = result.data;
      if (pickup.success === false) {
        warnings.push(pickup.errorMessage ?? "Pickup failed.");
        return formatToolResult({ result: pickup, suggestions, warnings });
      }

      suggestions.push(
        `Ship picked up successfully! New ship ID: ${pickup.shipId}.`
      );
      suggestions.push(
        "The new ship starts empty — no modules installed. " +
          "Obtain modules via psecs_manufacturing_overview (manufacture them) or psecs_market_search (buy them), " +
          "then use psecs_ship_manage_modules to install them."
      );
      suggestions.push(
        "Use psecs_fleet_status to see your updated fleet with the new ship."
      );

      return formatToolResult({
        result: pickup,
        suggestions,
        warnings,
      });
    }
  );
}
