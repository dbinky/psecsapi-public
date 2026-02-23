import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { PsecsClient } from "../client.js";
import { formatToolResult, formatToolError } from "../tool-utils.js";

interface ShipDetail {
  entityId?: string;
  ownerCorpId?: string;
  name?: string;
  class?: string;
  currentStructurePoints?: number;
  currentHullPoints?: number;
  maxStructurePoints?: number;
  maxHullPoints?: number;
  fleetId?: string;
  modules?: Array<{
    entityId?: string;
    name?: string;
    slotType?: string;
    category?: string;
    tier?: number;
    condition?: number;
    isEnabled?: boolean;
    interiorSlotsRequired?: number;
    exteriorSlotsRequired?: number;
    [key: string]: unknown;
  }>;
  capabilities?: unknown[];
  requirements?: unknown[];
  requirementsMet?: boolean;
  totalInteriorSlots?: number;
  totalExteriorSlots?: number;
  shipMass?: number;
  [key: string]: unknown;
}

interface ModuleInstallResult {
  success?: boolean;
  errorMessage?: string;
  installedModuleNames?: string[];
  interiorSlotsUsed?: number;
  exteriorSlotsUsed?: number;
  interiorSlotsAvailable?: number;
  exteriorSlotsAvailable?: number;
  [key: string]: unknown;
}

interface ModuleUninstallResult {
  success?: boolean;
  errorMessage?: string;
  uninstalledModuleNames?: string[];
  boxedModuleIds?: string[];
  [key: string]: unknown;
}

interface CargoItem {
  assetId: string;
  assetType?: string;
  name?: string;
  quantity?: number;
  mass?: number;
  [key: string]: unknown;
}

interface CargoInspectResult {
  assetId?: string;
  assetType?: string;
  name?: string;
  quantity?: number;
  mass?: number;
  resourceProperties?: Record<string, number>;
  componentQualities?: Record<string, number>;
  tier?: number;
  category?: string;
  definitionId?: string;
  slotType?: string;
  moduleCapabilities?: unknown[];
  moduleRequirements?: unknown[];
  alloyProperties?: Record<string, number>;
  [key: string]: unknown;
}

export function registerShipTools(
  server: McpServer,
  client: PsecsClient
): void {
  server.registerTool(
    "psecs_ship_manage_modules",
    {
      description:
        "Install or uninstall modules on a ship. For install, provide boxed module asset IDs " +
        "from cargo. For uninstall, provide installed module IDs and a destination cargo module ID. " +
        "Returns the result along with updated ship state and slot usage.",
      inputSchema: {
        shipId: z.string().describe("Ship ID to manage modules on"),
        action: z
          .enum(["install", "uninstall"])
          .describe("Whether to install or uninstall modules"),
        moduleIds: z
          .array(z.string())
          .describe(
            "For install: boxed module asset IDs from cargo. For uninstall: installed module IDs to remove."
          ),
        cargoModuleId: z
          .string()
          .optional()
          .describe(
            "Destination cargo module ID (required for uninstall — where uninstalled modules go)"
          ),
      },
    },
    async (args) => {
      const suggestions: string[] = [];
      const warnings: string[] = [];
      const pathOpts = { path: { shipId: args.shipId } };

      // Step 1: Get current ship state for context
      const shipResult = await client.get<ShipDetail>(
        "/api/Ship/{shipId}",
        pathOpts
      );
      if (!shipResult.ok) return formatToolError(shipResult);
      const ship = shipResult.data;

      if (args.action === "install") {
        // Step 2a: Install modules
        const installResult = await client.post<ModuleInstallResult>(
          "/api/Ship/{shipId}/install",
          { boxedModuleIds: args.moduleIds },
          pathOpts
        );
        if (!installResult.ok) return formatToolError(installResult);

        const result = installResult.data;
        if (result.success === false) {
          warnings.push(result.errorMessage ?? "Module installation failed.");
        } else {
          const names = result.installedModuleNames ?? [];
          if (names.length > 0) {
            suggestions.push(
              `Installed ${names.length} module(s): ${names.join(", ")}.`
            );
          }
          suggestions.push(
            `Slot usage — Interior: ${result.interiorSlotsUsed ?? "?"}/${(result.interiorSlotsUsed ?? 0) + (result.interiorSlotsAvailable ?? 0)}, ` +
              `Exterior: ${result.exteriorSlotsUsed ?? "?"}/${(result.exteriorSlotsUsed ?? 0) + (result.exteriorSlotsAvailable ?? 0)}.`
          );
        }

        suggestions.push(
          "Use psecs_fleet_status to see the updated fleet and ship overview."
        );
        if ((ship.modules?.length ?? 0) > 0) {
          suggestions.push(
            "modulesBeforeInstall shows module entityIds — use these when calling uninstall in the future."
          );
        }

        return formatToolResult({
          action: "install",
          result: result,
          ship: { entityId: ship.entityId, name: ship.name, class: ship.class },
          modulesBeforeInstall: ship.modules ?? [],
          suggestions,
          warnings,
        });
      } else {
        // Step 2b: Uninstall modules
        if (!args.cargoModuleId) {
          warnings.push(
            "cargoModuleId is required for uninstall — specify which cargo hold receives the uninstalled modules."
          );
          suggestions.push(
            "Use psecs_fleet_status to see the ship's current modules with their entityIds and cargo module IDs."
          );
          return formatToolResult({
            action: "uninstall",
            ship: { entityId: ship.entityId, name: ship.name, class: ship.class },
            currentModules: ship.modules ?? [],
            suggestions,
            warnings,
          });
        }

        const uninstallResult = await client.post<ModuleUninstallResult>(
          "/api/Ship/{shipId}/uninstall",
          { moduleIds: args.moduleIds, cargoModuleId: args.cargoModuleId },
          pathOpts
        );
        if (!uninstallResult.ok) return formatToolError(uninstallResult);

        const result = uninstallResult.data;
        if (result.success === false) {
          warnings.push(result.errorMessage ?? "Module uninstall failed.");
        } else {
          const names = result.uninstalledModuleNames ?? [];
          if (names.length > 0) {
            suggestions.push(
              `Uninstalled ${names.length} module(s): ${names.join(", ")}.`
            );
          }
          if (result.boxedModuleIds && result.boxedModuleIds.length > 0) {
            suggestions.push(
              "Uninstalled modules are now boxed assets in cargo. " +
                "Use psecs_ship_cargo_overview to see updated cargo, or psecs_market_sell to list them for sale."
            );
          }
        }

        suggestions.push(
          "Use psecs_fleet_status to see the updated fleet and ship overview."
        );

        return formatToolResult({
          action: "uninstall",
          result: result,
          ship: { entityId: ship.entityId, name: ship.name, class: ship.class },
          modulesBeforeUninstall: ship.modules ?? [],
          suggestions,
          warnings,
        });
      }
    }
  );

  server.registerTool(
    "psecs_ship_cargo_overview",
    {
      description:
        "Get a detailed overview of a ship's cargo hold contents. " +
        "Lists all cargo items and inspects the first 20 for detailed properties. " +
        "Returns cargo summary with space usage and suggestions.",
      inputSchema: {
        shipId: z.string().describe("Ship ID to inspect cargo for"),
      },
    },
    async (args) => {
      const suggestions: string[] = [];
      const warnings: string[] = [];
      const pathOpts = { path: { shipId: args.shipId } };

      // Step 1: Get cargo list
      const cargoResult = await client.get<CargoItem[]>(
        "/api/Ship/{shipId}/cargo",
        pathOpts
      );
      if (!cargoResult.ok) return formatToolError(cargoResult);
      const cargoItems = cargoResult.data;

      if (cargoItems.length === 0) {
        suggestions.push(
          "Cargo hold is empty. Mine resources with psecs_mine_resource or purchase items from the market with psecs_market_buy_or_bid."
        );
        return formatToolResult({
          cargoItems: [],
          inspectedItems: [],
          summary: { totalItems: 0, totalMass: 0 },
          suggestions,
          warnings,
        });
      }

      // Step 2: Inspect first 20 items for detailed properties
      const inspectLimit = Math.min(cargoItems.length, 20);
      const itemsToInspect = cargoItems.slice(0, inspectLimit);

      const inspectResults = await Promise.all(
        itemsToInspect.map((item) =>
          client.get<CargoInspectResult>(
            "/api/Ship/{shipId}/cargo/{assetId}/inspect",
            {
              path: { shipId: args.shipId, assetId: item.assetId },
            }
          )
        )
      );

      const inspectedItems: CargoInspectResult[] = [];
      for (const inspectResult of inspectResults) {
        if (inspectResult.ok) {
          inspectedItems.push(inspectResult.data);
        } else {
          warnings.push("Could not inspect a cargo item.");
        }
      }

      if (cargoItems.length > inspectLimit) {
        warnings.push(
          `Showing details for first ${inspectLimit} of ${cargoItems.length} items. ` +
            "Use psecs_raw_ship_cargo_inspect to inspect specific items."
        );
      }

      // Compute summary
      const totalMass = cargoItems.reduce(
        (sum, item) => sum + (item.mass ?? 0),
        0
      );

      // Group by asset type
      const typeCounts: Record<string, number> = {};
      for (const item of cargoItems) {
        const type = item.assetType ?? "Unknown";
        typeCounts[type] = (typeCounts[type] ?? 0) + 1;
      }

      // Generate suggestions based on cargo contents
      if (typeCounts["Resource"] && typeCounts["Resource"] > 0) {
        suggestions.push(
          `${typeCounts["Resource"]} resource(s) in cargo. Use psecs_market_sell to list for sale, or use them for manufacturing.`
        );
      }
      if (typeCounts["TechModule"] && typeCounts["TechModule"] > 0) {
        suggestions.push(
          `${typeCounts["TechModule"]} module(s) in cargo. Use psecs_ship_manage_modules to install them.`
        );
      }
      if (typeCounts["Component"] && typeCounts["Component"] > 0) {
        suggestions.push(
          `${typeCounts["Component"]} component(s) in cargo. Use psecs_manufacturing_overview to see which blueprints use these components.`
        );
      }
      if (typeCounts["Alloy"] && typeCounts["Alloy"] > 0) {
        suggestions.push(
          `${typeCounts["Alloy"]} alloy(s) in cargo. Alloys are used in ship building — use psecs_shipyard_browse to see chassis options.`
        );
      }

      return formatToolResult({
        cargoItems,
        inspectedItems,
        summary: {
          totalItems: cargoItems.length,
          totalMass,
          byType: typeCounts,
        },
        suggestions,
        warnings,
      });
    }
  );
}
