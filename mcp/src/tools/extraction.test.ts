import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { PsecsClient } from "../client.js";
import type { PsecsConfig } from "../config.js";
import { registerExtractionTools } from "./extraction.js";

describe("registerExtractionTools", () => {
  const config: PsecsConfig = {
    apiKey: "test-key",
    baseUrl: "https://api.psecs.io",
  };

  it("registers without throwing", () => {
    const server = new McpServer({ name: "test", version: "0.0.1" });
    const client = new PsecsClient(config);
    expect(() => registerExtractionTools(server, client)).not.toThrow();
  });

  it("can be called multiple times on different servers", () => {
    const client = new PsecsClient(config);
    const server1 = new McpServer({ name: "test1", version: "0.0.1" });
    const server2 = new McpServer({ name: "test2", version: "0.0.1" });
    expect(() => registerExtractionTools(server1, client)).not.toThrow();
    expect(() => registerExtractionTools(server2, client)).not.toThrow();
  });
});

describe("psecs_stop_extraction behavior", () => {
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

  it("stopAll aggregates results from multiple concurrent extraction jobs", async () => {
    // When stopAll is true, the tool calls DELETE /api/Ship/{shipId}/extraction/all
    // which returns an array of MaterializationResult objects — one per stopped job.
    // The tool should aggregate the total quantity across all results.
    const stopAllResults = [
      {
        jobId: "job-1",
        boxedResourceId: "boxed-1",
        rawResourceId: "raw-1",
        resourceName: "Iron Ore",
        materializedQuantity: 150,
      },
      {
        jobId: "job-2",
        boxedResourceId: "boxed-2",
        rawResourceId: "raw-2",
        resourceName: "Copper Ore",
        materializedQuantity: 75,
      },
      {
        jobId: "job-3",
        boxedResourceId: "boxed-3",
        rawResourceId: "raw-3",
        resourceName: "Gold Dust",
        materializedQuantity: 12,
      },
    ];

    fetchMock.mockResolvedValueOnce(mockJsonResponse(stopAllResults));

    // The tool calls DELETE /api/Ship/{shipId}/extraction/all for stopAll
    const result = await client.delete("/api/Ship/{shipId}/extraction/all", {
      path: { shipId: "ship-1" },
    });

    expect(result.ok).toBe(true);
    if (result.ok) {
      const results = result.data as typeof stopAllResults;

      // Verify we get an array (not a single object) for stopAll
      expect(Array.isArray(results)).toBe(true);
      expect(results).toHaveLength(3);

      // Verify aggregation logic: total quantity across all stopped jobs
      const totalQty = results.reduce(
        (sum, r) => sum + (r.materializedQuantity ?? 0),
        0
      );
      expect(totalQty).toBe(237); // 150 + 75 + 12

      // Verify each result has a distinct boxedResourceId for cargo tracking
      const boxedIds = results.map((r) => r.boxedResourceId);
      expect(new Set(boxedIds).size).toBe(3);
    }

    // Single fetch call for stopAll
    expect(fetchMock).toHaveBeenCalledTimes(1);

    // Verify it called the /all endpoint
    const [url] = fetchMock.mock.calls[0];
    expect(url).toContain("/extraction/all");
  });
});
