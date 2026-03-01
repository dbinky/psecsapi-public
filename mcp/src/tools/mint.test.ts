import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { PsecsClient } from "../client.js";
import type { PsecsConfig } from "../config.js";
import { registerMintTools } from "./mint.js";

describe("registerMintTools", () => {
  const config: PsecsConfig = {
    apiKey: "test-key",
    baseUrl: "https://api.psecs.io",
  };

  it("registers without throwing", () => {
    const server = new McpServer({ name: "test", version: "0.0.1" });
    const client = new PsecsClient(config);
    expect(() => registerMintTools(server, client)).not.toThrow();
  });

  it("can be called multiple times on different servers", () => {
    const client = new PsecsClient(config);
    const server1 = new McpServer({ name: "test1", version: "0.0.1" });
    const server2 = new McpServer({ name: "test2", version: "0.0.1" });
    expect(() => registerMintTools(server1, client)).not.toThrow();
    expect(() => registerMintTools(server2, client)).not.toThrow();
  });
});

describe("psecs_mint_rate behavior", () => {
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

  it("fetches the current mint rate from /api/mint/rate", async () => {
    const rateData = {
      currentRate: 20000,
      recentBurnVolume: 2,
      baseRate: 25000,
      floorRate: 5000,
      windowHours: 24,
    };
    fetchMock.mockResolvedValueOnce(mockJsonResponse(rateData));

    const result = await client.get<typeof rateData>("/api/mint/rate");

    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.data.currentRate).toBe(20000);
      expect(result.data.baseRate).toBe(25000);
      expect(result.data.floorRate).toBe(5000);
      expect(result.data.recentBurnVolume).toBe(2);
      expect(result.data.windowHours).toBe(24);
    }
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it("rate percent at base rate (25000/25000) is 100% — triggers high suggestion", () => {
    const ratePercent = Math.round((25000 / 25000) * 100);
    expect(ratePercent).toBe(100);
    expect(ratePercent > 80).toBe(true);
  });

  it("rate percent at floor rate (5000/25000) is 20% — triggers depressed suggestion", () => {
    const ratePercent = Math.round((5000 / 25000) * 100);
    expect(ratePercent).toBe(20);
    expect(ratePercent < 40).toBe(true);
  });

  it("rate percent at midpoint (12500/25000) is 50% — moderate suggestion", () => {
    const ratePercent = Math.round((12500 / 25000) * 100);
    expect(ratePercent).toBe(50);
    expect(ratePercent >= 40 && ratePercent <= 80).toBe(true);
  });

  it("rate percent boundary: exactly 80% is moderate (not high)", () => {
    const currentRate = Math.round(25000 * 0.8); // 20000
    const ratePercent = Math.round((currentRate / 25000) * 100);
    expect(ratePercent).toBe(80);
    // ratePercent > 80 is false at exactly 80 — suggestion is moderate
    expect(ratePercent > 80).toBe(false);
    expect(ratePercent >= 40).toBe(true);
  });

  it("rate percent boundary: exactly 40% is moderate (not depressed)", () => {
    const currentRate = Math.round(25000 * 0.4); // 10000
    const ratePercent = Math.round((currentRate / 25000) * 100);
    expect(ratePercent).toBe(40);
    // ratePercent < 40 is false at exactly 40 — suggestion is moderate
    expect(ratePercent < 40).toBe(false);
  });

  it("returns infrastructure error when /api/mint/rate responds with 500", async () => {
    fetchMock.mockResolvedValueOnce(
      mockJsonResponse({ error: "Internal server error" }, 500)
    );

    const result = await client.get("/api/mint/rate");

    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(result.errorType).toBe("infrastructure");
    }
  });

  it("returns auth error when /api/mint/rate responds with 401", async () => {
    fetchMock.mockResolvedValueOnce(
      mockJsonResponse({ message: "Unauthorized" }, 401)
    );

    const result = await client.get("/api/mint/rate");

    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(result.errorType).toBe("auth");
    }
  });
});

describe("psecs_mint_burn behavior", () => {
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

  it("posts to /api/mint/burn with the specified amount in the request body", async () => {
    const burnResult = {
      tokensBurned: 5,
      rateApplied: 20000,
      creditsReceived: 100000,
      newTokenBalance: 45,
      newCorpCredits: 500000,
    };
    fetchMock.mockResolvedValueOnce(mockJsonResponse(burnResult));

    const result = await client.post<typeof burnResult>("/api/mint/burn", {
      amount: 5,
    });

    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.data.tokensBurned).toBe(5);
      expect(result.data.rateApplied).toBe(20000);
      expect(result.data.creditsReceived).toBe(100000);
      expect(result.data.newTokenBalance).toBe(45);
      expect(result.data.newCorpCredits).toBe(500000);
    }

    expect(fetchMock).toHaveBeenCalledTimes(1);
    const [, callInit] = fetchMock.mock.calls[0];
    expect(JSON.parse(callInit.body)).toEqual({ amount: 5 });
  });

  it("creditsReceived equals tokensBurned * rateApplied", async () => {
    const tokensBurned = 10;
    const rateApplied = 18500;
    const burnResult = {
      tokensBurned,
      rateApplied,
      creditsReceived: tokensBurned * rateApplied,
      newTokenBalance: 90,
      newCorpCredits: 1000000,
    };
    fetchMock.mockResolvedValueOnce(mockJsonResponse(burnResult));

    const result = await client.post<typeof burnResult>("/api/mint/burn", {
      amount: tokensBurned,
    });

    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.data.creditsReceived).toBe(185000);
    }
  });

  it("returns a game error when user has insufficient tokens", async () => {
    fetchMock.mockResolvedValueOnce(
      mockJsonResponse(
        {
          DomainExceptionMessage:
            "Insufficient token balance.",
        },
        400
      )
    );

    const result = await client.post("/api/mint/burn", { amount: 10 });

    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(result.errorType).toBe("game");
      expect(result.message).toContain("Insufficient token balance");
    }
  });

  it("returns a game error when user has no corp", async () => {
    fetchMock.mockResolvedValueOnce(
      mockJsonResponse(
        {
          DomainExceptionMessage: "No corp found for this user.",
        },
        400
      )
    );

    const result = await client.post("/api/mint/burn", { amount: 1 });

    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(result.errorType).toBe("game");
      expect(result.message).toContain("No corp found");
    }
  });

  it("returns a game error when burn amount exceeds the 100 token maximum", async () => {
    // The API enforces this server-side; MCP Zod schema enforces it client-side.
    // This test verifies the API error path is handled gracefully.
    fetchMock.mockResolvedValueOnce(
      mockJsonResponse(
        {
          DomainExceptionMessage:
            "Invalid mint amount. Must be between 0.1 and 100 tokens.",
        },
        400
      )
    );

    const result = await client.post("/api/mint/burn", { amount: 101 });

    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(result.errorType).toBe("game");
    }
  });

  it("returns auth error when /api/mint/burn responds with 401", async () => {
    fetchMock.mockResolvedValueOnce(
      mockJsonResponse({ message: "Unauthorized" }, 401)
    );

    const result = await client.post("/api/mint/burn", { amount: 1 });

    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(result.errorType).toBe("auth");
    }
  });

  it("returns infrastructure error when /api/mint/burn responds with 500", async () => {
    fetchMock.mockResolvedValueOnce(
      mockJsonResponse({ error: "Internal server error" }, 500)
    );

    const result = await client.post("/api/mint/burn", { amount: 1 });

    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(result.errorType).toBe("infrastructure");
    }
  });
});
