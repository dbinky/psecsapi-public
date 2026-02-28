import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { PsecsClient } from "../client.js";
import { formatToolResult, formatToolError } from "../tool-utils.js";

interface WarehouseItem {
  assetId?: string;
  assetType?: string;
  assetName?: string;
  mass?: number;
  tier?: string;
  depositedAt?: string;
  dailyRate?: number;
  pickupWindowDays?: number;
  depositPaid?: number;
  depositRemaining?: number;
  nextBillingTime?: string;
  gracePeriodStart?: string | null;
  [key: string]: unknown;
}

interface WarehouseContents {
  items?: WarehouseItem[];
  totalMassStored?: number;
  freeTierCapacity?: number;
  freeTierUsed?: number;
  paidTierUsed?: number;
  [key: string]: unknown;
}

interface WarehouseSummary {
  totalItems?: number;
  totalMassStored?: number;
  freeTierCapacity?: number;
  freeTierUsed?: number;
  paidTierUsed?: number;
  dailyBillingTotal?: number;
  itemsInGracePeriod?: number;
  [key: string]: unknown;
}

interface WarehouseDepositResponse {
  success?: boolean;
  tier?: string;
  massDeposited?: number;
  creditsCharged?: number;
  errorMessage?: string;
  [key: string]: unknown;
}

interface WarehouseWithdrawResponse {
  success?: boolean;
  creditsRefunded?: number;
  itemsPromoted?: number;
  promotionRefund?: number;
  errorMessage?: string;
  [key: string]: unknown;
}

function formatItem(item: WarehouseItem) {
  return {
    assetId: item.assetId,
    assetType: item.assetType,
    assetName: item.assetName,
    mass: item.mass,
    tier: item.tier,
    depositedAt: item.depositedAt,
    dailyRate: item.dailyRate,
    pickupWindowDays: item.pickupWindowDays,
    depositPaid: item.depositPaid,
    depositRemaining: item.depositRemaining,
    nextBillingTime: item.nextBillingTime,
    gracePeriodStart: item.gracePeriodStart ?? null,
  };
}

export function registerWarehouseTools(
  server: McpServer,
  client: PsecsClient
): void {
  server.registerTool(
    "psecs_warehouse_list",
    {
      description:
        "View your corp's Nexus Warehouse contents and billing status. " +
        "The warehouse persists boxed assets (resources, modules, alloys) between extraction runs. " +
        "Free tier: first 10,000 mass units stored indefinitely at no cost. " +
        "Paid tier: 1 credit/mass/day with a deposit charged upfront (dailyRate × pickupWindowDays); " +
        "remaining deposit refunds on withdrawal. " +
        "Items in grace period have missed billing and are at risk of deletion — act immediately. " +
        "Use this before planning what to extract, sell, or move to assess storage capacity.",
      inputSchema: {},
    },
    async (_args) => {
      const suggestions: string[] = [];
      const warnings: string[] = [];

      const [contentsResult, summaryResult] = await Promise.all([
        client.get<WarehouseContents>("/api/warehouse"),
        client.get<WarehouseSummary>("/api/warehouse/summary"),
      ]);

      if (!contentsResult.ok) return formatToolError(contentsResult);
      if (!summaryResult.ok) return formatToolError(summaryResult);

      const contents = contentsResult.data;
      const summary = summaryResult.data;

      const freeTierCapacity = contents.freeTierCapacity ?? 10000;
      const freeTierAvailable = freeTierCapacity - (contents.freeTierUsed ?? 0);

      if ((summary.itemsInGracePeriod ?? 0) > 0) {
        warnings.push(
          `${summary.itemsInGracePeriod} item(s) are in grace period — billing failed and they risk deletion. ` +
            "Withdraw immediately or ensure your corp has sufficient credits."
        );
      }

      if (freeTierAvailable > 0) {
        suggestions.push(
          `Free tier has ${freeTierAvailable.toFixed(0)} mass units available (${freeTierCapacity.toFixed(0)} capacity). ` +
            "Use psecs_warehouse_deposit to store assets at no cost."
        );
      } else {
        suggestions.push(
          "Free tier is full. Paid tier deposits require pickupWindowDays (min 1) and charge mass × days credits upfront."
        );
      }

      if ((summary.dailyBillingTotal ?? 0) > 0) {
        suggestions.push(
          `Daily billing: ${summary.dailyBillingTotal} credits/day for paid tier items. ` +
            "Withdraw items no longer needed to stop charges."
        );
      }

      if ((contents.items?.length ?? 0) === 0) {
        suggestions.push(
          "Warehouse is empty. Use psecs_warehouse_deposit to store assets from ship cargo."
        );
        return formatToolResult({
          totalItems: 0,
          freeTierCapacity,
          freeTierUsed: contents.freeTierUsed,
          freeTierAvailable,
          paidTierUsed: contents.paidTierUsed,
          dailyBillingTotal: summary.dailyBillingTotal,
          freeItems: [],
          paidItems: [],
          warnings,
          suggestions,
        });
      }

      const freeItems = (contents.items ?? []).filter(
        (i) => i.tier === "Free"
      );
      const paidItems = (contents.items ?? []).filter(
        (i) => i.tier === "Paid"
      );

      suggestions.push(
        "Use psecs_warehouse_withdraw with assetId, fleetId, shipId, and cargoModuleId to retrieve an item."
      );

      return formatToolResult({
        totalItems: contents.items?.length ?? 0,
        freeTierCapacity,
        freeTierUsed: contents.freeTierUsed,
        freeTierAvailable,
        paidTierUsed: contents.paidTierUsed,
        dailyBillingTotal: summary.dailyBillingTotal,
        itemsInGracePeriod: summary.itemsInGracePeriod,
        freeItems: freeItems.map(formatItem),
        paidItems: paidItems.map(formatItem),
        warnings,
        suggestions,
      });
    }
  );

  server.registerTool(
    "psecs_warehouse_deposit",
    {
      description:
        "Deposit a boxed asset from ship cargo into the Nexus Warehouse. " +
        "Requires your fleet to be docked at a Nexus sector and idle. " +
        "Free tier (no cost, no expiry) is used automatically while capacity remains (10,000 mass total). " +
        "Paid tier costs 1 credit/mass/day — specify pickupWindowDays (minimum 1). " +
        "Paid tier charge: mass × pickupWindowDays credits upfront; remaining deposit refunds on withdrawal. " +
        "Use psecs_ship_cargo_overview to find assetId and assetType in your cargo.",
      inputSchema: {
        assetId: z
          .string()
          .describe("ID of the boxed asset to deposit (from ship cargo)"),
        assetType: z
          .enum(["Resource", "TechModule", "Alloy", "Component", "Chassis"])
          .describe("Type of the asset (use the exact assetType value shown in psecs_ship_cargo_overview)"),
        fleetId: z
          .string()
          .describe("Fleet ID — fleet must be in a Nexus sector and idle"),
        shipId: z
          .string()
          .describe(
            "Ship ID within the fleet that currently holds the asset in cargo"
          ),
        pickupWindowDays: z
          .number()
          .int()
          .min(1)
          .optional()
          .describe(
            "Required only when free tier is full. Number of days deposit covers (minimum 1). " +
              "More days = higher upfront deposit but longer window before billing fails."
          ),
      },
    },
    async (args) => {
      const suggestions: string[] = [];
      const warnings: string[] = [];

      const depositResult = await client.post<WarehouseDepositResponse>(
        "/api/warehouse/deposit",
        {
          assetId: args.assetId,
          assetType: args.assetType,
          fleetId: args.fleetId,
          shipId: args.shipId,
          pickupWindowDays: args.pickupWindowDays,
        }
      );

      if (!depositResult.ok) return formatToolError(depositResult);
      const result = depositResult.data;

      if (!result.success) {
        if (
          result.errorMessage?.toLowerCase().includes("free tier") ||
          result.errorMessage?.toLowerCase().includes("capacity")
        ) {
          warnings.push(
            "Free tier is full. Specify pickupWindowDays (e.g., 7) to deposit into paid tier."
          );
        }
        return formatToolResult({
          success: false,
          errorMessage: result.errorMessage,
          warnings,
          suggestions: [
            "Check psecs_warehouse_list to see free tier availability.",
            "Use pickupWindowDays to deposit into paid tier when free tier is full.",
          ],
        });
      }

      if (result.tier === "Free") {
        suggestions.push(
          "Asset stored in free tier — no charges, no expiry. Use psecs_warehouse_withdraw any time."
        );
      } else {
        const dailyRate = result.massDeposited ?? 0;
        suggestions.push(
          `Asset stored in paid tier. Daily rate: ${dailyRate} credits/day. ` +
            "Withdraw before pickup window expires to receive remaining deposit refund."
        );
        suggestions.push(
          "Use psecs_warehouse_list to monitor deposit remaining and grace period status."
        );
      }

      return formatToolResult({
        success: true,
        tier: result.tier,
        massDeposited: result.massDeposited,
        creditsCharged: result.creditsCharged,
        warnings,
        suggestions,
      });
    }
  );

  server.registerTool(
    "psecs_warehouse_withdraw",
    {
      description:
        "Retrieve a stored asset from the Nexus Warehouse into ship cargo. " +
        "Requires your fleet to be docked at a Nexus sector and idle. " +
        "Paid tier items: remaining deposit is refunded to corp credits on withdrawal. " +
        "After withdrawal, paid items waiting behind freed free-tier space are auto-promoted (oldest first), " +
        "and their remaining deposits are also refunded. " +
        "Use psecs_warehouse_list to find asset IDs and psecs_ship_cargo_overview to find cargoModuleId.",
      inputSchema: {
        assetId: z
          .string()
          .describe(
            "ID of the warehouse asset to withdraw (from psecs_warehouse_list)"
          ),
        fleetId: z
          .string()
          .describe("Fleet ID — fleet must be in a Nexus sector and idle"),
        shipId: z
          .string()
          .describe("Ship ID to receive the withdrawn asset"),
        cargoModuleId: z
          .string()
          .describe(
            "Cargo module ID on the target ship where the asset will be placed"
          ),
      },
    },
    async (args) => {
      const suggestions: string[] = [];
      const warnings: string[] = [];

      const withdrawResult = await client.post<WarehouseWithdrawResponse>(
        "/api/warehouse/withdraw",
        {
          assetId: args.assetId,
          fleetId: args.fleetId,
          shipId: args.shipId,
          cargoModuleId: args.cargoModuleId,
        }
      );

      if (!withdrawResult.ok) return formatToolError(withdrawResult);
      const result = withdrawResult.data;

      if (!result.success) {
        return formatToolResult({
          success: false,
          errorMessage: result.errorMessage,
          warnings,
          suggestions: [
            "Ensure your fleet is in a Nexus sector and idle.",
            "Check psecs_ship_cargo_overview for cargoModuleId and available capacity.",
            "Use psecs_warehouse_list to confirm the asset ID.",
          ],
        });
      }

      if ((result.creditsRefunded ?? 0) > 0) {
        suggestions.push(
          `Deposit refund: ${result.creditsRefunded} credits returned to corp account.`
        );
      }

      if ((result.itemsPromoted ?? 0) > 0) {
        const promotionNote =
          (result.promotionRefund ?? 0) > 0
            ? ` — ${result.promotionRefund} additional credits refunded from their deposits.`
            : ".";
        suggestions.push(
          `${result.itemsPromoted} paid tier item(s) promoted to free tier${promotionNote}`
        );
      }

      suggestions.push(
        "Use psecs_warehouse_list to review remaining warehouse contents and billing status."
      );

      return formatToolResult({
        success: true,
        creditsRefunded: result.creditsRefunded,
        itemsPromoted: result.itemsPromoted,
        promotionRefund: result.promotionRefund,
        warnings,
        suggestions,
      });
    }
  );
}
