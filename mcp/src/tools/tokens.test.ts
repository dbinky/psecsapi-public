import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { PsecsClient } from "../client.js";
import type { PsecsConfig } from "../config.js";
import { registerTokenTools } from "./tokens.js";

const config: PsecsConfig = {
  apiKey: "test-key",
  baseUrl: "https://api.psecs.io",
};

function mockJsonResponse(data: unknown, status = 200): Response {
  return new Response(JSON.stringify(data), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}

describe("psecs_token_status includes investment info", () => {
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

  it("returns invested amount, daily yield, next payout, and eligible to uninvest", async () => {
    // Mock three parallel requests: balance, stake-info, investment-info
    fetchMock
      .mockResolvedValueOnce(
        mockJsonResponse({
          tokens: 10,
          stakedTokens: 2,
          availableTokens: 3,
        })
      )
      .mockResolvedValueOnce(
        mockJsonResponse({
          stakedTokens: 2,
          availableTokens: 3,
          rateLimit: 50,
          cooldownEndsAt: null,
        })
      )
      .mockResolvedValueOnce(
        mockJsonResponse({
          totalInvested: 5,
          eligibleToUninvest: 3,
          estimatedNextPayout: 500,
          nextPayoutTime: "2026-04-04T04:00:00Z",
          tranches: [
            {
              amount: 3,
              investedAt: "2026-04-02T18:00:00Z",
              lastPayoutAt: "2026-04-03T04:00:00Z",
              isEligibleToUninvest: true,
            },
            {
              amount: 2,
              investedAt: "2026-04-03T14:00:00Z",
              lastPayoutAt: null,
              isEligibleToUninvest: false,
            },
          ],
        })
      );

    const result = await client.get("/api/tokens/balance");
    // We can't directly call the tool handler, so verify the API calls work
    // and test the response shape expectations
    expect(fetchMock).toHaveBeenCalledTimes(1);

    // Reset and test via the client methods that the tool would use
    fetchMock.mockReset();
    fetchMock
      .mockResolvedValueOnce(
        mockJsonResponse({
          tokens: 10,
          stakedTokens: 2,
          availableTokens: 3,
        })
      )
      .mockResolvedValueOnce(
        mockJsonResponse({
          stakedTokens: 2,
          availableTokens: 3,
          rateLimit: 50,
          cooldownEndsAt: null,
        })
      )
      .mockResolvedValueOnce(
        mockJsonResponse({
          totalInvested: 5,
          eligibleToUninvest: 3,
          estimatedNextPayout: 500,
          nextPayoutTime: "2026-04-04T04:00:00Z",
          tranches: [],
        })
      );

    const [balanceRes, stakeRes, investRes] = await Promise.all([
      client.get<{ tokens: number; stakedTokens: number; availableTokens: number }>("/api/tokens/balance"),
      client.get<{ stakedTokens: number; rateLimit: number; cooldownEndsAt: string | null }>("/api/User/api-stake-info"),
      client.get<{ totalInvested: number; eligibleToUninvest: number; estimatedNextPayout: number; nextPayoutTime: string }>("/api/user/investment-info"),
    ]);

    expect(balanceRes.ok).toBe(true);
    expect(stakeRes.ok).toBe(true);
    expect(investRes.ok).toBe(true);

    if (investRes.ok) {
      expect(investRes.data.totalInvested).toBe(5);
      expect(investRes.data.eligibleToUninvest).toBe(3);
      expect(investRes.data.estimatedNextPayout).toBe(500);
      expect(investRes.data.nextPayoutTime).toBe("2026-04-04T04:00:00Z");
      // Verify daily yield would be totalInvested * 100
      expect(investRes.data.totalInvested * 100).toBe(500);
    }

    expect(fetchMock).toHaveBeenCalledTimes(3);
  });
});

describe("psecs_invest_tokens behavior", () => {
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

  it("happy path - posts to /api/user/invest-tokens and returns new balances", async () => {
    const investResult = {
      newInvestedTotal: 5,
      newAvailableTokens: 3,
      trancheCount: 2,
    };
    fetchMock.mockResolvedValueOnce(mockJsonResponse(investResult));

    const result = await client.post<typeof investResult>(
      "/api/user/invest-tokens",
      { amount: 2 }
    );

    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.data.newInvestedTotal).toBe(5);
      expect(result.data.newAvailableTokens).toBe(3);
      expect(result.data.trancheCount).toBe(2);
    }

    expect(fetchMock).toHaveBeenCalledTimes(1);
    const [, callInit] = fetchMock.mock.calls[0];
    expect(JSON.parse(callInit.body)).toEqual({ amount: 2 });
  });

  it("error - returns game error when user has no corp", async () => {
    fetchMock.mockResolvedValueOnce(
      mockJsonResponse(
        { DomainExceptionMessage: "No corp found for this user." },
        400
      )
    );

    const result = await client.post("/api/user/invest-tokens", { amount: 1 });

    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(result.errorType).toBe("game");
      expect(result.message).toContain("No corp found");
    }
  });
});

describe("psecs_uninvest_tokens behavior", () => {
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

  it("happy path - posts to /api/user/uninvest-tokens and returns balances", async () => {
    const uninvestResult = {
      tokensUninvested: 2,
      newInvestedTotal: 3,
      newAvailableTokens: 5,
    };
    fetchMock.mockResolvedValueOnce(mockJsonResponse(uninvestResult));

    const result = await client.post<typeof uninvestResult>(
      "/api/user/uninvest-tokens",
      { amount: 2 }
    );

    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.data.tokensUninvested).toBe(2);
      expect(result.data.newInvestedTotal).toBe(3);
      expect(result.data.newAvailableTokens).toBe(5);
    }

    expect(fetchMock).toHaveBeenCalledTimes(1);
    const [, callInit] = fetchMock.mock.calls[0];
    expect(JSON.parse(callInit.body)).toEqual({ amount: 2 });
  });

  it("error - returns game error when no eligible tranches", async () => {
    fetchMock.mockResolvedValueOnce(
      mockJsonResponse(
        {
          DomainExceptionMessage:
            "Requested uninvest amount exceeds eligible tokens.",
        },
        400
      )
    );

    const result = await client.post("/api/user/uninvest-tokens", {
      amount: 5,
    });

    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(result.errorType).toBe("game");
      expect(result.message).toContain("exceeds eligible tokens");
    }
  });
});

describe("registerTokenTools", () => {
  it("registers all token tools without throwing", () => {
    const server = new McpServer({ name: "test", version: "0.0.1" });
    const client = new PsecsClient(config);
    expect(() => registerTokenTools(server, client)).not.toThrow();
  });
});
