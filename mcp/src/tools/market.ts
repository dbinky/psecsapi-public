import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { PsecsClient } from "../client.js";
import { formatToolResult, formatToolError } from "../tool-utils.js";

interface MarketListingItem {
  saleId: string;
  type?: string;
  sellerCorpId?: string;
  sellerCorpName?: string;
  assetSummary?: string;
  price?: number;
  startingPrice?: number;
  bidCount?: number;
  createdAt?: string;
  expiresAt?: string;
  timeRemaining?: string;
  description?: string;
  [key: string]: unknown;
}

interface MarketListingResponse {
  listings?: MarketListingItem[];
  page?: number;
  pageSize?: number;
  totalItems?: number;
  totalPages?: number;
  [key: string]: unknown;
}

interface MyBidsItem {
  saleId: string;
  type?: string;
  sellerCorpName?: string;
  assetSummary?: string;
  currentHighBid?: number;
  bidCount?: number;
  expiresAt?: string;
  timeRemaining?: string;
  yourBidAmount?: number;
  bidStatus?: string;
  [key: string]: unknown;
}

interface SaleDetails {
  saleId: string;
  type?: string;
  state?: string;
  sellerCorpId?: string;
  sellerCorpName?: string;
  buyerCorpId?: string;
  buyerCorpName?: string;
  boxedAssetId?: string;
  assetSummary?: string;
  price?: number;
  startingPrice?: number;
  bidCount?: number;
  minimumNextBid?: number;
  description?: string;
  durationDays?: number;
  createdAt?: string;
  expiresAt?: string;
  pickupWindowEndsAt?: string;
  timeRemaining?: string;
  storageFeesPaid?: number;
  [key: string]: unknown;
}

interface SaleResult {
  success?: boolean;
  errorCode?: string;
  errorMessage?: string;
  saleId?: string;
  newState?: string;
  feesCharged?: number;
  creditsTransferred?: number;
  expiresAt?: string;
  pickupWindowEndsAt?: string;
  [key: string]: unknown;
}

export function registerMarketTools(
  server: McpServer,
  client: PsecsClient
): void {
  server.registerTool(
    "psecs_market_search",
    {
      description:
        "Search the Nexus Market for listings with optional filters. " +
        "Also shows your active bids alongside search results for context. " +
        "Returns combined listing and bid data with suggestions.",
      inputSchema: {
        type: z
          .enum(["BuyNow", "Auction"])
          .optional()
          .describe("Filter by sale type: BuyNow or Auction"),
        assetType: z
          .string()
          .optional()
          .describe("Filter by asset type (e.g., resource, module, alloy)"),
        minPrice: z.number().optional().describe("Minimum price filter"),
        maxPrice: z.number().optional().describe("Maximum price filter"),
        sort: z
          .string()
          .optional()
          .describe("Sort field: price, time, newest, or bids"),
        page: z.number().optional().describe("Page number (default 1)"),
        limit: z.number().optional().describe("Results per page (default 20)"),
      },
    },
    async (args) => {
      const suggestions: string[] = [];
      const warnings: string[] = [];

      // Build query params
      const query: Record<string, string | number | boolean | undefined> = {};
      if (args.type) query.type = args.type.toLowerCase();
      if (args.assetType) query.assetType = args.assetType;
      if (args.minPrice !== undefined) query.minPrice = args.minPrice;
      if (args.maxPrice !== undefined) query.maxPrice = args.maxPrice;
      if (args.sort) query.sort = args.sort;
      if (args.page !== undefined) query.page = args.page;
      if (args.limit !== undefined) query.limit = args.limit;

      // Parallel fetch: market listings and user's active bids
      const [listingsResult, bidsResult] = await Promise.all([
        client.get<MarketListingResponse>("/api/market", { query }),
        client.get<MyBidsItem[]>("/api/market/my-bids"),
      ]);

      if (!listingsResult.ok) return formatToolError(listingsResult);

      const listings = listingsResult.data;
      const bids = bidsResult.ok ? bidsResult.data : null;

      if (!bidsResult.ok) warnings.push("Could not fetch your active bids.");

      const items = listings.listings ?? [];
      if (items.length === 0) {
        suggestions.push(
          "No listings match your search criteria. Try broadening your filters or check back later."
        );
      } else {
        const auctions = items.filter((i) => i.type === "Auction");
        const buyNow = items.filter((i) => i.type === "BuyNow");

        if (buyNow.length > 0) {
          suggestions.push(
            `${buyNow.length} BuyNow listing(s) available. Use psecs_market_buy_or_bid with a saleId to purchase instantly.`
          );
        }
        if (auctions.length > 0) {
          suggestions.push(
            `${auctions.length} auction(s) available. Use psecs_market_buy_or_bid with saleId and amount to place a bid.`
          );
        }

        if (listings.totalPages && listings.totalPages > (listings.page ?? 1)) {
          suggestions.push(
            `Page ${listings.page ?? 1} of ${listings.totalPages}. Use the page parameter to browse more results.`
          );
        }
      }

      // Show bid context
      if (bids && bids.length > 0) {
        const outbid = bids.filter((b) => b.bidStatus === "Outbid");
        const winning = bids.filter((b) => b.bidStatus === "Winning");

        if (outbid.length > 0) {
          warnings.push(
            `You've been outbid on ${outbid.length} auction(s). Use psecs_market_buy_or_bid to raise your bid.`
          );
        }
        if (winning.length > 0) {
          suggestions.push(
            `You're winning ${winning.length} auction(s). Monitor with psecs_market_portfolio.`
          );
        }
      }

      return formatToolResult({
        listings,
        myBids: bids,
        suggestions,
        warnings,
      });
    }
  );

  server.registerTool(
    "psecs_market_sell",
    {
      description:
        "List an item for sale on the Nexus Market. The item must be a boxed asset " +
        "in a ship's cargo hold. Supports both BuyNow (fixed price) and Auction listings. " +
        "BuyNow charges a storage fee of 1% × price × duration days, paid upfront at creation (non-refundable). " +
        "Auctions charge a storage fee of 0.5% × final sale price × duration days, deducted from seller proceeds on completion.",
      inputSchema: {
        shipId: z.string().describe("Ship ID containing the item to sell"),
        boxedAssetId: z
          .string()
          .describe("Boxed asset ID from the ship's cargo"),
        price: z
          .number()
          .describe("Sale price (fixed price for BuyNow, starting price for Auction)"),
        durationDays: z
          .number()
          .min(1)
          .describe("How many days the listing should remain active"),
        isAuction: z
          .boolean()
          .default(false)
          .optional()
          .describe("Create an auction instead of BuyNow (default false)"),
        description: z
          .string()
          .optional()
          .describe("Optional description for the listing"),
      },
    },
    async (args) => {
      const suggestions: string[] = [];
      const warnings: string[] = [];

      const body = {
        shipId: args.shipId,
        boxedAssetId: args.boxedAssetId,
        price: args.price,
        durationDays: args.durationDays,
        isAuction: args.isAuction ?? false,
        description: args.description ?? "",
      };

      const result = await client.post<SaleResult>("/api/market", body);
      if (!result.ok) return formatToolError(result);

      const saleData = result.data;
      if (saleData.success === false) {
        warnings.push(saleData.errorMessage ?? "Sale creation failed.");
        return formatToolResult({ result: saleData, suggestions, warnings });
      }

      const saleType = args.isAuction ? "Auction" : "BuyNow";
      suggestions.push(
        `${saleType} listing created successfully. Sale ID: ${saleData.saleId}`
      );

      if (saleData.expiresAt) {
        suggestions.push(`Listing expires at: ${saleData.expiresAt}`);
      }

      if (saleData.feesCharged) {
        const feeNote = args.isAuction
          ? "(0.5% of final price × duration days — deducted from proceeds at completion)"
          : "(1% of price × duration days — non-refundable, charged upfront)";
        suggestions.push(
          `Storage fee: ${saleData.feesCharged} credits ${feeNote}.`
        );
      }

      suggestions.push(
        "Use psecs_market_portfolio to monitor your active sales and bids."
      );

      return formatToolResult({
        result: saleData,
        suggestions,
        warnings,
      });
    }
  );

  server.registerTool(
    "psecs_market_buy_or_bid",
    {
      description:
        "Purchase a BuyNow listing or place a bid on an auction. " +
        "Automatically detects the sale type and takes the appropriate action. " +
        "For auctions, the amount parameter is required.",
      inputSchema: {
        saleId: z.string().describe("Sale ID to purchase or bid on"),
        amount: z
          .number()
          .optional()
          .describe("Bid amount (required for auctions, ignored for BuyNow)"),
      },
    },
    async (args) => {
      const suggestions: string[] = [];
      const warnings: string[] = [];

      // Step 1: Get sale details to determine type
      const detailsResult = await client.get<SaleDetails>(
        "/api/market/{saleId}",
        { path: { saleId: args.saleId } }
      );
      if (!detailsResult.ok) return formatToolError(detailsResult);

      const details = detailsResult.data;
      const saleType = details.type;

      if (details.state !== "Open") {
        warnings.push(
          `Sale is not open (state: ${details.state}). It may have already been purchased, expired, or cancelled.`
        );
        return formatToolResult({
          saleDetails: details,
          suggestions,
          warnings,
        });
      }

      let result;

      if (saleType === "BuyNow") {
        // Step 2a: Purchase BuyNow
        const purchaseResult = await client.post<SaleResult>(
          "/api/market/{saleId}/purchase",
          undefined,
          { path: { saleId: args.saleId } }
        );
        if (!purchaseResult.ok) return formatToolError(purchaseResult);
        result = purchaseResult.data;

        if (result.success) {
          suggestions.push(
            `Purchase successful! ${result.creditsTransferred ? `Paid ${result.creditsTransferred} credits.` : ""}`
          );
          if (result.pickupWindowEndsAt) {
            suggestions.push(
              `Pick up your item before: ${result.pickupWindowEndsAt}. ` +
                "Use psecs_market_retrieve to collect it to a ship."
            );
          }
        } else {
          warnings.push(result.errorMessage ?? "Purchase failed.");
        }
      } else if (saleType === "Auction") {
        // Step 2b: Place bid on auction
        if (args.amount === undefined) {
          warnings.push(
            "This is an auction — the amount parameter is required to place a bid."
          );
          if (details.minimumNextBid) {
            suggestions.push(`Minimum bid: ${details.minimumNextBid} credits.`);
          }
          return formatToolResult({
            saleDetails: details,
            suggestions,
            warnings,
          });
        }

        if (details.minimumNextBid && args.amount < details.minimumNextBid) {
          warnings.push(
            `Bid amount ${args.amount} is below the minimum next bid of ${details.minimumNextBid}.`
          );
          return formatToolResult({
            saleDetails: details,
            suggestions,
            warnings,
          });
        }

        const bidResult = await client.post<SaleResult>(
          "/api/market/{saleId}/bid",
          { amount: args.amount },
          { path: { saleId: args.saleId } }
        );
        if (!bidResult.ok) return formatToolError(bidResult);
        result = bidResult.data;

        if (result.success) {
          suggestions.push(
            `Bid of ${args.amount} credits placed successfully on "${details.assetSummary ?? args.saleId}".`
          );
          if (details.timeRemaining) {
            suggestions.push(
              `Auction ends in: ${details.timeRemaining}. Late bids trigger a 5-minute anti-snipe extension.`
            );
          }
          suggestions.push(
            "Use psecs_market_portfolio to track your bid status."
          );
        } else {
          warnings.push(result.errorMessage ?? "Bid failed.");
        }
      } else {
        warnings.push(`Unknown sale type: ${saleType}. Cannot proceed.`);
        return formatToolResult({
          saleDetails: details,
          suggestions,
          warnings,
        });
      }

      return formatToolResult({
        saleDetails: details,
        result,
        suggestions,
        warnings,
      });
    }
  );

  server.registerTool(
    "psecs_market_portfolio",
    {
      description:
        "Get a combined view of your market activity including active sales and bids. " +
        "Returns portfolio summary with suggestions about expiring sales, outbid items, and items awaiting pickup.",
    },
    async () => {
      const suggestions: string[] = [];
      const warnings: string[] = [];

      // Parallel fetch: my sales and my bids
      const [salesResult, bidsResult] = await Promise.all([
        client.get<MarketListingResponse>("/api/market/my-sales"),
        client.get<MyBidsItem[]>("/api/market/my-bids"),
      ]);

      const sales = salesResult.ok ? salesResult.data : null;
      const bids = bidsResult.ok ? bidsResult.data : null;

      if (!salesResult.ok) warnings.push("Could not fetch your sales.");
      if (!bidsResult.ok) warnings.push("Could not fetch your bids.");

      // Analyze sales
      if (sales) {
        const listings = sales.listings ?? [];
        if (listings.length === 0) {
          suggestions.push(
            "No active sales. Use psecs_market_sell to list items from your ship cargo."
          );
        } else {
          const auctions = listings.filter((l) => l.type === "Auction");
          const buyNow = listings.filter((l) => l.type === "BuyNow");

          if (buyNow.length > 0) {
            suggestions.push(`${buyNow.length} active BuyNow listing(s).`);
          }
          if (auctions.length > 0) {
            const withBids = auctions.filter((a) => (a.bidCount ?? 0) > 0);
            suggestions.push(
              `${auctions.length} active auction(s), ${withBids.length} with bids.`
            );
          }

          // Check for expiring soon (items with short timeRemaining)
          // We can only do basic string checks since timeRemaining is a formatted string
          const expiringSoon = listings.filter((l) => {
            const tr = l.timeRemaining ?? "";
            return tr.includes("hour") || tr.includes("minute") || tr.startsWith("0d");
          });
          if (expiringSoon.length > 0) {
            warnings.push(
              `${expiringSoon.length} listing(s) expiring soon. Use psecs_raw_create_market_repost to renew expired listings, or psecs_raw_create_market_cancel to pull them early.`
            );
          }
        }
      }

      // Analyze bids
      if (bids) {
        if (bids.length === 0) {
          suggestions.push(
            "No active bids. Use psecs_market_search to find items to bid on."
          );
        } else {
          const outbid = bids.filter((b) => b.bidStatus === "Outbid");
          const winning = bids.filter((b) => b.bidStatus === "Winning");
          const won = bids.filter((b) => b.bidStatus === "Won");

          if (outbid.length > 0) {
            const outbidDetails = outbid.map(
              (b) =>
                `"${b.assetSummary ?? b.saleId}" (current high: ${b.currentHighBid}, your bid: ${b.yourBidAmount})`
            );
            warnings.push(
              `Outbid on ${outbid.length} auction(s): ${outbidDetails.join("; ")}. ` +
                "Use psecs_market_buy_or_bid to raise your bid."
            );
          }

          if (winning.length > 0) {
            suggestions.push(
              `Winning ${winning.length} auction(s). Keep monitoring — late bids can trigger anti-snipe extensions.`
            );
          }

          if (won.length > 0) {
            warnings.push(
              `${won.length} auction(s) won and awaiting pickup! ` +
                "Use psecs_market_retrieve to collect items before the pickup window expires."
            );
          }
        }
      }

      return formatToolResult({
        sales,
        bids,
        suggestions,
        warnings,
      });
    }
  );

  server.registerTool(
    "psecs_market_retrieve",
    {
      description:
        "Retrieve a purchased or won auction item from the market, delivering it to a ship's cargo hold. " +
        "Use after a BuyNow purchase or winning an auction. " +
        "The fleet containing the destination ship must be at the Nexus sector. " +
        "Get saleId from psecs_market_buy_or_bid or psecs_market_portfolio. " +
        "Get shipId and cargoModuleId from psecs_fleet_status or psecs_ship_cargo_overview.",
      inputSchema: {
        saleId: z.string().describe("Sale ID of the purchased item (from market buy/bid result or portfolio)"),
        shipId: z.string().describe("Ship ID to deliver the item to"),
        cargoModuleId: z.string().describe("Cargo module ID on the destination ship"),
      },
    },
    async (args) => {
      const suggestions: string[] = [];
      const warnings: string[] = [];

      const result = await client.post<SaleResult>(
        "/api/market/{saleId}/retrieve",
        { ShipId: args.shipId, CargoModuleId: args.cargoModuleId },
        { path: { saleId: args.saleId } }
      );
      if (!result.ok) return formatToolError(result);

      const data = result.data;
      if (data.success === false) {
        warnings.push(data.errorMessage ?? "Retrieval failed.");
        return formatToolResult({ result: data, suggestions, warnings });
      }

      suggestions.push("Item retrieved successfully and added to your ship's cargo.");
      suggestions.push(
        "Use psecs_ship_cargo_overview to confirm the item is in your cargo hold."
      );
      suggestions.push(
        "Use psecs_market_portfolio to check for other items awaiting pickup."
      );

      return formatToolResult({ result: data, suggestions, warnings });
    }
  );
}
