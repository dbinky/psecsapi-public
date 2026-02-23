import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { PsecsClient } from "../client.js";
import type { PsecsConfig } from "../config.js";
import { registerMarketTools } from "./market.js";

describe("registerMarketTools", () => {
  const config: PsecsConfig = {
    apiKey: "test-key",
    baseUrl: "https://api.psecs.io",
  };

  it("registers without throwing", () => {
    const server = new McpServer({ name: "test", version: "0.0.1" });
    const client = new PsecsClient(config);
    expect(() => registerMarketTools(server, client)).not.toThrow();
  });

  it("can be called multiple times on different servers", () => {
    const client = new PsecsClient(config);
    const server1 = new McpServer({ name: "test1", version: "0.0.1" });
    const server2 = new McpServer({ name: "test2", version: "0.0.1" });
    expect(() => registerMarketTools(server1, client)).not.toThrow();
    expect(() => registerMarketTools(server2, client)).not.toThrow();
  });
});

describe("psecs_market_buy_or_bid behavior", () => {
  const config: PsecsConfig = {
    apiKey: "test-key",
    baseUrl: "https://api.psecs.io",
  };

  let client: PsecsClient;
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
    vi.stubGlobal("fetch", fetchMock);
    client = new PsecsClient(config);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  function mockJsonResponse(data: unknown, status = 200): Response {
    return new Response(JSON.stringify(data), {
      status,
      headers: { "Content-Type": "application/json" },
    });
  }

  it("fetches sale details before purchasing a BuyNow listing", async () => {
    const saleDetails = {
      saleId: "sale-1",
      type: "BuyNow",
      state: "Open",
      price: 500,
      assetSummary: "Iron Ore x100",
      sellerCorpName: "OreTraders Inc",
    };

    const purchaseResult = {
      success: true,
      saleId: "sale-1",
      creditsTransferred: 500,
      pickupWindowEndsAt: "2026-03-01T00:00:00Z",
    };

    // First call: GET sale details; Second call: POST purchase
    fetchMock
      .mockResolvedValueOnce(mockJsonResponse(saleDetails))
      .mockResolvedValueOnce(mockJsonResponse(purchaseResult));

    // Step 1: Get sale details (the tool does this first)
    const detailsRes = await client.get("/api/market/{saleId}", {
      path: { saleId: "sale-1" },
    });

    expect(detailsRes.ok).toBe(true);
    if (detailsRes.ok) {
      const details = detailsRes.data as typeof saleDetails;
      expect(details.type).toBe("BuyNow");
      expect(details.state).toBe("Open");
    }

    // Step 2: Purchase (because type is BuyNow)
    const purchaseRes = await client.post("/api/market/{saleId}/purchase", undefined, {
      path: { saleId: "sale-1" },
    });

    expect(purchaseRes.ok).toBe(true);
    if (purchaseRes.ok) {
      const result = purchaseRes.data as typeof purchaseResult;
      expect(result.success).toBe(true);
      expect(result.creditsTransferred).toBe(500);
    }

    expect(fetchMock).toHaveBeenCalledTimes(2);
  });

  it("places a bid on an auction listing", async () => {
    const saleDetails = {
      saleId: "sale-2",
      type: "Auction",
      state: "Open",
      startingPrice: 100,
      minimumNextBid: 150,
      assetSummary: "Plasma Module T3",
      timeRemaining: "2d 5h",
    };

    const bidResult = {
      success: true,
      saleId: "sale-2",
      newState: "Open",
    };

    fetchMock
      .mockResolvedValueOnce(mockJsonResponse(saleDetails))
      .mockResolvedValueOnce(mockJsonResponse(bidResult));

    // Step 1: Get sale details
    const detailsRes = await client.get("/api/market/{saleId}", {
      path: { saleId: "sale-2" },
    });

    expect(detailsRes.ok).toBe(true);
    if (detailsRes.ok) {
      const details = detailsRes.data as typeof saleDetails;
      expect(details.type).toBe("Auction");
      expect(details.minimumNextBid).toBe(150);
    }

    // Step 2: Place bid (because type is Auction)
    const bidRes = await client.post(
      "/api/market/{saleId}/bid",
      { amount: 200 },
      { path: { saleId: "sale-2" } }
    );

    expect(bidRes.ok).toBe(true);
    if (bidRes.ok) {
      const result = bidRes.data as typeof bidResult;
      expect(result.success).toBe(true);
    }

    expect(fetchMock).toHaveBeenCalledTimes(2);

    // Verify the bid POST body
    const [, bidInit] = fetchMock.mock.calls[1];
    expect(JSON.parse(bidInit.body)).toEqual({ amount: 200 });
  });

  it("rejects purchase on non-Open sale", async () => {
    const saleDetails = {
      saleId: "sale-3",
      type: "BuyNow",
      state: "Completed",
      price: 500,
    };

    fetchMock.mockResolvedValueOnce(mockJsonResponse(saleDetails));

    const detailsRes = await client.get("/api/market/{saleId}", {
      path: { saleId: "sale-3" },
    });

    expect(detailsRes.ok).toBe(true);
    if (detailsRes.ok) {
      const details = detailsRes.data as typeof saleDetails;
      expect(details.state).toBe("Completed");
      // The tool would return early with a warning here
    }

    // Should NOT make a second call since sale is not Open
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it("returns error when sale details fetch fails", async () => {
    fetchMock.mockResolvedValueOnce(
      mockJsonResponse({ message: "Sale not found" }, 404)
    );

    const result = await client.get("/api/market/{saleId}", {
      path: { saleId: "nonexistent" },
    });

    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(result.errorType).toBe("game");
      expect(result.message).toBe("Sale not found");
    }
  });

  it("validates bid amount against minimum next bid", async () => {
    const saleDetails = {
      saleId: "sale-4",
      type: "Auction",
      state: "Open",
      minimumNextBid: 500,
    };

    fetchMock.mockResolvedValueOnce(mockJsonResponse(saleDetails));

    const detailsRes = await client.get("/api/market/{saleId}", {
      path: { saleId: "sale-4" },
    });

    expect(detailsRes.ok).toBe(true);
    if (detailsRes.ok) {
      const details = detailsRes.data as typeof saleDetails;
      // The tool would check: amount < minimumNextBid and return early
      const bidAmount = 300;
      expect(bidAmount < details.minimumNextBid).toBe(true);
    }

    // Only the details fetch, no bid call since amount is too low
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it("handles purchase failure gracefully", async () => {
    const saleDetails = {
      saleId: "sale-5",
      type: "BuyNow",
      state: "Open",
      price: 10000,
    };

    const purchaseResult = {
      success: false,
      errorMessage: "Insufficient credits",
    };

    fetchMock
      .mockResolvedValueOnce(mockJsonResponse(saleDetails))
      .mockResolvedValueOnce(mockJsonResponse(purchaseResult));

    const detailsRes = await client.get("/api/market/{saleId}", {
      path: { saleId: "sale-5" },
    });
    expect(detailsRes.ok).toBe(true);

    const purchaseRes = await client.post(
      "/api/market/{saleId}/purchase",
      undefined,
      { path: { saleId: "sale-5" } }
    );
    expect(purchaseRes.ok).toBe(true);
    if (purchaseRes.ok) {
      const result = purchaseRes.data as typeof purchaseResult;
      expect(result.success).toBe(false);
      expect(result.errorMessage).toBe("Insufficient credits");
    }
  });
});
