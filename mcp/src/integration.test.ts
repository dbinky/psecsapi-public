/**
 * Integration tests — run against a live PSECS dev instance.
 *
 * Prerequisites:
 *   1. PSECS API running at http://localhost:5130
 *   2. PSECS_API_KEY set to a valid dev API key
 *   3. User has a corporation with at least one fleet
 *
 * Run: PSECS_API_KEY=psecs_sk_xxx PSECS_BASE_URL=http://localhost:5130 npx vitest run src/integration.test.ts
 */
import { describe, it, expect } from "vitest";
import { PsecsClient } from "./client.js";
import { loadConfig } from "./config.js";

const SKIP = !process.env.PSECS_API_KEY || !process.env.PSECS_BASE_URL;

describe.skipIf(SKIP)("integration tests", () => {
  const config = SKIP
    ? { apiKey: "", baseUrl: "" }
    : loadConfig();
  const client = new PsecsClient(config);

  it("fetches user profile", async () => {
    const result = await client.get("/api/User");
    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.data).toHaveProperty("userId");
    }
  });

  it("fetches research status", async () => {
    const result = await client.get("/api/Research/status");
    expect(result.ok).toBe(true);
  });

  it("lists market", async () => {
    const result = await client.get("/api/Market");
    expect(result.ok).toBe(true);
  });

  it("rejects invalid API key", async () => {
    const badClient = new PsecsClient({
      apiKey: "psecs_sk_invalid",
      baseUrl: config.baseUrl,
    });
    const result = await badClient.get("/api/User");
    expect(result.ok).toBe(false);
    if (!result.ok) {
      expect(result.errorType).toBe("auth");
    }
  });
});
