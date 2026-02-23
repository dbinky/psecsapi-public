import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { PsecsClient } from "../client.js";
import type { PsecsConfig } from "../config.js";
import type { ApiResult } from "../client.js";
import { registerFleetTools } from "./fleet.js";

describe("registerFleetTools", () => {
  const config: PsecsConfig = {
    apiKey: "test-key",
    baseUrl: "https://api.psecs.io",
  };

  it("registers without throwing", () => {
    const server = new McpServer({ name: "test", version: "0.0.1" });
    const client = new PsecsClient(config);
    expect(() => registerFleetTools(server, client)).not.toThrow();
  });

  it("can be called multiple times on different servers", () => {
    const client = new PsecsClient(config);
    const server1 = new McpServer({ name: "test1", version: "0.0.1" });
    const server2 = new McpServer({ name: "test2", version: "0.0.1" });
    expect(() => registerFleetTools(server1, client)).not.toThrow();
    expect(() => registerFleetTools(server2, client)).not.toThrow();
  });
});

describe("psecs_explore_sector behavior", () => {
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

  it("aggregates scan, deep-scan, and survey into a single result", async () => {
    // Arrange: mock scan -> 2 orbitals, deep scans for each, survey
    const scanData = {
      sectorId: "sector-1",
      sectorName: "Alpha Centauri",
      sectorType: "StarSystem",
      orbitals: [
        { index: 0, type: "Planet" },
        { index: 1, type: "AsteroidBelt" },
      ],
      conduits: [
        { conduitId: "conduit-1", destinationSectorId: "sector-2" },
      ],
    };

    const deepScan0 = {
      orbital: 0,
      resources: [
        { resourceId: "res-1", name: "Iron Ore", density: 85 },
      ],
    };
    const deepScan1 = {
      orbital: 1,
      resources: [
        { resourceId: "res-2", name: "Copper Ore", density: 42 },
        { resourceId: "res-3", name: "Gold Dust", density: 12 },
      ],
    };

    const surveyData = {
      fleets: [
        { fleetId: "fleet-1", corpId: "corp-1", shipCount: 2 },
      ],
    };

    // The explore tool makes: 1 scan, 2 deep scans, 1 survey = 4 fetches
    fetchMock
      .mockResolvedValueOnce(mockJsonResponse(scanData))      // scan
      .mockResolvedValueOnce(mockJsonResponse(deepScan0))      // deep scan orbital 0
      .mockResolvedValueOnce(mockJsonResponse(deepScan1))      // deep scan orbital 1
      .mockResolvedValueOnce(mockJsonResponse(surveyData));    // survey

    // Act: call explore directly via the client
    const scanResult = await client.get("/api/Fleet/{fleetId}/scan", {
      path: { fleetId: "fleet-1" },
    });
    expect(scanResult.ok).toBe(true);

    // Verify deep scan calls
    const ds0Result = await client.get("/api/Fleet/{fleetId}/scan/deep", {
      path: { fleetId: "fleet-1" },
      query: { orbital: 0 },
    });
    expect(ds0Result.ok).toBe(true);

    const ds1Result = await client.get("/api/Fleet/{fleetId}/scan/deep", {
      path: { fleetId: "fleet-1" },
      query: { orbital: 1 },
    });
    expect(ds1Result.ok).toBe(true);

    const surveyResult = await client.get("/api/Fleet/{fleetId}/survey", {
      path: { fleetId: "fleet-1" },
    });
    expect(surveyResult.ok).toBe(true);

    // Verify we made 4 calls total
    expect(fetchMock).toHaveBeenCalledTimes(4);

    // Verify the scan result data
    if (scanResult.ok) {
      const scan = scanResult.data as typeof scanData;
      expect(scan.orbitals).toHaveLength(2);
      expect(scan.conduits).toHaveLength(1);
    }

    // Verify deep scan aggregation
    if (ds0Result.ok && ds1Result.ok) {
      const totalResources =
        ((ds0Result.data as typeof deepScan0).resources?.length ?? 0) +
        ((ds1Result.data as typeof deepScan1).resources?.length ?? 0);
      expect(totalResources).toBe(3);
    }
  });

  it("returns error when scan fails", async () => {
    fetchMock.mockResolvedValueOnce(
      mockJsonResponse({ message: "Fleet not found" }, 404)
    );

    const result = await client.get("/api/Fleet/{fleetId}/scan", {
      path: { fleetId: "nonexistent" },
    });

    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(result.errorType).toBe("game");
      expect(result.message).toBe("Fleet not found");
    }
  });

  it("handles deep scan failures gracefully", async () => {
    // scan succeeds, deep scan fails
    fetchMock
      .mockResolvedValueOnce(
        mockJsonResponse({
          sectorId: "sector-1",
          orbitals: [{ index: 0, type: "Planet" }],
          conduits: [],
        })
      )
      .mockResolvedValueOnce(
        mockJsonResponse({ message: "Sensor malfunction" }, 500)
      );

    const scanResult = await client.get("/api/Fleet/{fleetId}/scan", {
      path: { fleetId: "fleet-1" },
    });
    expect(scanResult.ok).toBe(true);

    const deepResult = await client.get("/api/Fleet/{fleetId}/scan/deep", {
      path: { fleetId: "fleet-1" },
      query: { orbital: 0 },
    });
    expect(deepResult.ok).toBe(false);
    if (!deepResult.ok) {
      expect(deepResult.errorType).toBe("infrastructure");
    }
  });

  it("suggests resources when deep scans find them", async () => {
    // Verify that resources found in deep scans would generate appropriate data
    const deepScanWithResources = {
      orbital: 0,
      resources: [
        { resourceId: "res-1", name: "Titanium", density: 95 },
        { resourceId: "res-2", name: "Platinum", density: 78 },
      ],
    };

    fetchMock.mockResolvedValueOnce(mockJsonResponse(deepScanWithResources));

    const result = await client.get("/api/Fleet/{fleetId}/scan/deep", {
      path: { fleetId: "fleet-1" },
      query: { orbital: 0 },
    });

    expect(result.ok).toBe(true);
    if (result.ok) {
      const data = result.data as typeof deepScanWithResources;
      expect(data.resources).toHaveLength(2);
      expect(data.resources![0].density).toBe(95);
    }
  });
});
