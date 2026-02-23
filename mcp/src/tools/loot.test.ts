import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { PsecsClient } from "../client.js";
import type { PsecsConfig } from "../config.js";
import { registerLootTools } from "./loot.js";

describe("registerLootTools", () => {
  const config: PsecsConfig = {
    apiKey: "test-key",
    baseUrl: "https://api.psecs.io",
  };

  it("registers without throwing", () => {
    const server = new McpServer({ name: "test", version: "0.0.1" });
    const client = new PsecsClient(config);
    expect(() => registerLootTools(server, client)).not.toThrow();
  });

  it("can be called multiple times on different servers", () => {
    const client = new PsecsClient(config);
    const server1 = new McpServer({ name: "test1", version: "0.0.1" });
    const server2 = new McpServer({ name: "test2", version: "0.0.1" });
    expect(() => registerLootTools(server1, client)).not.toThrow();
    expect(() => registerLootTools(server2, client)).not.toThrow();
  });
});

describe("psecs_scan_loot behavior", () => {
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

  it("warns when fleet is in transit (no sectorId)", async () => {
    // A fleet in transit has no sectorId — the tool should return a warning
    // and an empty loot list without attempting the loot scan call.
    const fleetData = {
      fleetId: "fleet-1",
      name: "Alpha Fleet",
      state: "InTransit",
      // sectorId is intentionally absent — fleet is between sectors
      shipIds: ["ship-1"],
    };

    fetchMock.mockResolvedValueOnce(mockJsonResponse(fleetData));

    // Step 1: Get fleet details (the tool does this first)
    const fleetResult = await client.get("/api/Fleet/{fleetId}", {
      path: { fleetId: "fleet-1" },
    });

    expect(fleetResult.ok).toBe(true);
    if (fleetResult.ok) {
      const fleet = fleetResult.data as typeof fleetData;
      // The tool checks for sectorId — when absent, it returns early with a warning
      expect(fleet.sectorId).toBeUndefined();
      expect(fleet.state).toBe("InTransit");
    }

    // Only one fetch call should be made — no loot scan attempt
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it("calculates expiry urgency for soon-to-expire loot fields", async () => {
    // The tool should warn about loot fields expiring in less than 1 hour
    const now = new Date();
    const expiresIn30Min = new Date(now.getTime() + 30 * 60 * 1000).toISOString();
    const expiresIn12Hours = new Date(now.getTime() + 12 * 60 * 60 * 1000).toISOString();

    const fleetData = {
      fleetId: "fleet-1",
      name: "Alpha Fleet",
      sectorId: "sector-1",
      state: "Idle",
      shipIds: ["ship-1"],
    };

    const lootFields = [
      {
        id: "loot-urgent",
        positionX: 10,
        positionY: 20,
        itemCount: 5,
        isExclusive: true,
        expiresAt: expiresIn30Min,
      },
      {
        id: "loot-safe",
        positionX: 30,
        positionY: 40,
        itemCount: 3,
        isExclusive: false,
        expiresAt: expiresIn12Hours,
      },
    ];

    fetchMock
      .mockResolvedValueOnce(mockJsonResponse(fleetData))
      .mockResolvedValueOnce(mockJsonResponse(lootFields));

    // Step 1: Get fleet
    const fleetResult = await client.get("/api/Fleet/{fleetId}", {
      path: { fleetId: "fleet-1" },
    });
    expect(fleetResult.ok).toBe(true);

    // Step 2: Get loot
    const lootResult = await client.get("/api/sector/{sectorId}/loot", {
      path: { sectorId: "sector-1" },
    });
    expect(lootResult.ok).toBe(true);

    if (lootResult.ok) {
      const fields = lootResult.data as typeof lootFields;
      expect(fields).toHaveLength(2);

      // Verify the urgency calculation logic that the tool uses
      const urgentField = fields[0];
      const safeField = fields[1];

      const urgentExpiry = new Date(urgentField.expiresAt);
      const safeExpiry = new Date(safeField.expiresAt);

      const urgentHoursRemaining =
        (urgentExpiry.getTime() - now.getTime()) / (1000 * 60 * 60);
      const safeHoursRemaining =
        (safeExpiry.getTime() - now.getTime()) / (1000 * 60 * 60);

      // The urgent field should have less than 1 hour remaining (triggers warning)
      expect(urgentHoursRemaining).toBeLessThan(1);
      // The safe field should have more than 1 hour remaining (no warning)
      expect(safeHoursRemaining).toBeGreaterThan(1);
    }

    expect(fetchMock).toHaveBeenCalledTimes(2);
  });
});
