import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { PsecsClient } from "../client.js";
import { formatToolResult, formatToolError } from "../tool-utils.js";

interface CargoTransferResult {
  success?: boolean;
  errorMessage?: string;
  [key: string]: unknown;
}

export function registerCargoTools(
  server: McpServer,
  client: PsecsClient
): void {
  server.registerTool(
    "psecs_cargo_transfer",
    {
      description:
        "Transfer a cargo item (boxed resource, module, or alloy) between two ships in the same fleet. " +
        "Requires the fleet ID, source and destination ship IDs, the boxed asset ID to transfer, " +
        "and the destination cargo module ID (which hold on the destination ship receives the item). " +
        "Use psecs_ship_cargo_overview to find boxed asset IDs and cargo module IDs before transferring.",
      inputSchema: {
        fleetId: z
          .string()
          .describe("Fleet ID containing both ships"),
        sourceShipId: z
          .string()
          .describe("Ship ID to transfer the item FROM"),
        destinationShipId: z
          .string()
          .describe("Ship ID to transfer the item TO"),
        boxedAssetId: z
          .string()
          .describe(
            "Boxed asset ID of the cargo item to transfer (from psecs_ship_cargo_overview)"
          ),
        destinationCargoModuleId: z
          .string()
          .describe(
            "Cargo module ID on the destination ship that will receive the item (from psecs_ship_cargo_overview)"
          ),
      },
    },
    async (args) => {
      const suggestions: string[] = [];
      const warnings: string[] = [];

      // Execute the transfer
      const transferResult = await client.post<CargoTransferResult>(
        "/api/cargo/transfer",
        {
          BoxedAssetId: args.boxedAssetId,
          SourceShipId: args.sourceShipId,
          DestinationShipId: args.destinationShipId,
          DestinationCargoModuleId: args.destinationCargoModuleId,
        },
        { query: { fleetId: args.fleetId } }
      );
      if (!transferResult.ok) return formatToolError(transferResult);

      const result = transferResult.data;

      if (result.success === false) {
        warnings.push(result.errorMessage ?? "Transfer failed.");
        suggestions.push(
          "Common issues: asset not found in source ship, destination cargo module full, or ships not in the same fleet."
        );
        suggestions.push(
          "Use psecs_ship_cargo_overview on both ships to verify asset IDs and cargo module IDs."
        );
      } else {
        suggestions.push("Cargo transferred successfully.");
        suggestions.push(
          `Use psecs_ship_cargo_overview with shipId "${args.destinationShipId}" to verify the item arrived.`
        );
        suggestions.push(
          `Use psecs_ship_cargo_overview with shipId "${args.sourceShipId}" to see remaining cargo.`
        );
      }

      suggestions.push(
        "Use psecs_cargo_move to reorganize items between cargo holds on the same ship."
      );

      return formatToolResult({
        action: "transfer",
        fleetId: args.fleetId,
        sourceShipId: args.sourceShipId,
        destinationShipId: args.destinationShipId,
        boxedAssetId: args.boxedAssetId,
        result,
        suggestions,
        warnings,
      });
    }
  );

  server.registerTool(
    "psecs_cargo_move",
    {
      description:
        "Move a cargo item between holds (cargo modules) on the same ship. " +
        "Useful for reorganizing cargo — e.g., moving items to a larger hold " +
        "or freeing space in a specific module. " +
        "Use psecs_ship_cargo_overview to find boxed asset IDs and cargo module IDs.",
      inputSchema: {
        shipId: z
          .string()
          .describe("Ship ID containing the cargo item"),
        boxedAssetId: z
          .string()
          .describe(
            "Boxed asset ID of the cargo item to move (from psecs_ship_cargo_overview)"
          ),
        destinationCargoModuleId: z
          .string()
          .describe(
            "Cargo module ID on the same ship to move the item into (from psecs_ship_cargo_overview)"
          ),
      },
    },
    async (args) => {
      const suggestions: string[] = [];
      const warnings: string[] = [];

      // Execute the move
      const moveResult = await client.post<CargoTransferResult>(
        "/api/cargo/move",
        {
          BoxedAssetId: args.boxedAssetId,
          DestinationCargoModuleId: args.destinationCargoModuleId,
        },
        { query: { shipId: args.shipId } }
      );
      if (!moveResult.ok) return formatToolError(moveResult);

      const result = moveResult.data;

      if (result.success === false) {
        warnings.push(result.errorMessage ?? "Move failed.");
        suggestions.push(
          "Common issues: asset not found in ship cargo, destination module full, or invalid module ID."
        );
        suggestions.push(
          "Use psecs_ship_cargo_overview to verify asset and cargo module IDs."
        );
      } else {
        suggestions.push("Cargo moved successfully between holds.");
        suggestions.push(
          `Use psecs_ship_cargo_overview with shipId "${args.shipId}" to see updated cargo layout.`
        );
      }

      suggestions.push(
        "Use psecs_cargo_transfer to move items between different ships in the same fleet."
      );

      return formatToolResult({
        action: "move",
        shipId: args.shipId,
        boxedAssetId: args.boxedAssetId,
        destinationCargoModuleId: args.destinationCargoModuleId,
        result,
        suggestions,
        warnings,
      });
    }
  );
}
