import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { PsecsClient } from "../client.js";
import { formatToolResult, formatToolError } from "../tool-utils.js";

interface EngageCombatResult {
  success?: boolean;
  combatId?: string;
  errorMessage?: string;
  [key: string]: unknown;
}

interface CombatStatus {
  combatId?: string;
  status?: string;
  [key: string]: unknown;
}

interface CombatSummary {
  combatId?: string;
  attackerCorpId?: string;
  defenderCorpId?: string;
  attackerFleetId?: string;
  defenderFleetId?: string;
  outcome?: string;
  durationTicks?: number;
  durationSeconds?: number;
  shipsDestroyed?: string[];
  shipsFled?: string[];
  timestamp?: string;
  [key: string]: unknown;
}

export function registerCombatTools(
  server: McpServer,
  client: PsecsClient
): void {
  server.registerTool(
    "psecs_engage_combat",
    {
      description:
        "Initiate fleet-vs-fleet combat. The attacker fleet must belong to your corp. " +
        "Both fleets must be Idle and in the same non-Nexus sector. " +
        "IMPORTANT: Assign a combat script to your fleet BEFORE engaging, or your ships will " +
        "default to flee behavior. Use psecs_raw_create_corp_scripts to write a script, then " +
        "psecs_raw_update_fleet_combat_script to assign it. See the psecs://guide/combat-scripting " +
        "resource for the full scripting API. " +
        "Combat runs asynchronously — use psecs_combat_status to monitor progress.",
      inputSchema: {
        attackerFleetId: z
          .string()
          .describe("Your fleet ID that will initiate the attack"),
        targetFleetId: z
          .string()
          .describe("Enemy fleet ID to attack (must be in the same sector)"),
      },
    },
    async (args) => {
      const suggestions: string[] = [];
      const warnings: string[] = [];

      const result = await client.post<EngageCombatResult>(
        "/api/combat/engage",
        {
          attackerFleetId: args.attackerFleetId,
          targetFleetId: args.targetFleetId,
        }
      );
      if (!result.ok) return formatToolError(result);

      const engagement = result.data;
      if (engagement.success === false) {
        warnings.push(
          engagement.errorMessage ?? "Combat engagement failed."
        );
        return formatToolResult({ engagement, suggestions, warnings });
      }

      suggestions.push(
        `Combat initiated! Combat ID: ${engagement.combatId}. ` +
          "Use psecs_combat_status to monitor the battle."
      );
      suggestions.push(
        "Combat runs asynchronously via simulation. Check status periodically until it completes."
      );
      warnings.push(
        "Your fleet is now in combat and cannot navigate or perform other actions until combat resolves."
      );

      return formatToolResult({
        engagement,
        suggestions,
        warnings,
      });
    }
  );

  server.registerTool(
    "psecs_combat_status",
    {
      description:
        "Check the current status of a combat instance. " +
        "Returns whether combat is still in progress or has completed. " +
        "If completed, use psecs_combat_summary for full results.",
      inputSchema: {
        combatId: z
          .string()
          .describe("Combat instance ID (from psecs_engage_combat result)"),
      },
    },
    async (args) => {
      const suggestions: string[] = [];
      const warnings: string[] = [];

      const result = await client.get<CombatStatus>(
        "/api/combat/{combatId}/status",
        { path: { combatId: args.combatId } }
      );
      if (!result.ok) return formatToolError(result);

      const status = result.data;
      const currentStatus = status.status ?? "Unknown";

      if (currentStatus === "InProgress") {
        suggestions.push(
          "Combat is still in progress. Check again in a few seconds with psecs_combat_status."
        );
        warnings.push(
          "Fleets involved cannot perform other actions while combat is active."
        );
      } else if (currentStatus === "Completed") {
        suggestions.push(
          "Combat is complete! Use psecs_combat_summary to see the full battle results, including outcome and casualties."
        );
      } else {
        suggestions.push(
          `Combat status: ${currentStatus}. Use psecs_combat_summary if the battle has concluded.`
        );
      }

      return formatToolResult({
        status,
        suggestions,
        warnings,
      });
    }
  );

  server.registerTool(
    "psecs_combat_summary",
    {
      description:
        "Get the full summary of a completed combat instance. " +
        "Includes outcome (AttackerWon, DefenderWon, Draw, TimedOut), " +
        "ships destroyed and fled, and duration. Summary data persists indefinitely.",
      inputSchema: {
        combatId: z
          .string()
          .describe("Combat instance ID to get summary for"),
      },
    },
    async (args) => {
      const suggestions: string[] = [];
      const warnings: string[] = [];

      const result = await client.get<CombatSummary>(
        "/api/combat/{combatId}/summary",
        { path: { combatId: args.combatId } }
      );
      if (!result.ok) return formatToolError(result);

      const summary = result.data;
      const outcome = summary.outcome ?? "Unknown";

      // Generate outcome-specific suggestions
      if (outcome === "AttackerWon") {
        suggestions.push(
          "Attacker won the battle. Destroyed ships drop loot fields — the victor has exclusive pickup for 1 hour."
        );
        suggestions.push(
          "If you are the attacker: use psecs_scan_loot with your fleet ID to find loot fields, then psecs_pickup_loot to collect them."
        );
      } else if (outcome === "DefenderWon") {
        suggestions.push(
          "Defender won the battle. Use psecs_fleet_status to assess fleet damage."
        );
        suggestions.push(
          "If you are the defender: use psecs_scan_loot with your fleet ID to find loot from any destroyed attacker ships."
        );
        suggestions.push(
          "Consider retreating to a safer sector with psecs_navigate if your fleet is weakened."
        );
      } else if (outcome === "Draw") {
        suggestions.push(
          "Combat ended in a draw. Both fleets may have sustained damage."
        );
        suggestions.push(
          "Use psecs_fleet_status to check your fleet's condition."
        );
      } else if (outcome === "TimedOut") {
        suggestions.push(
          "Combat timed out — neither side achieved a decisive victory."
        );
        suggestions.push(
          "Use psecs_fleet_status to assess damage, then decide whether to re-engage or retreat."
        );
      }

      // Report casualties
      const destroyed = summary.shipsDestroyed ?? [];
      const fled = summary.shipsFled ?? [];
      if (destroyed.length > 0) {
        warnings.push(
          `${destroyed.length} ship(s) were destroyed in this battle.`
        );
      }
      if (fled.length > 0) {
        suggestions.push(
          `${fled.length} ship(s) fled the combat grid.`
        );
      }

      if (summary.durationSeconds !== undefined) {
        suggestions.push(
          `Battle lasted ${summary.durationSeconds.toFixed(1)} seconds (${summary.durationTicks ?? "?"} ticks).`
        );
      }

      return formatToolResult({
        summary,
        suggestions,
        warnings,
      });
    }
  );
}
