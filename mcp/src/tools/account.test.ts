import { describe, it, expect } from "vitest";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { PsecsClient } from "../client.js";
import type { PsecsConfig } from "../config.js";
import { registerAccountTools } from "./account.js";

describe("registerAccountTools", () => {
  const config: PsecsConfig = {
    apiKey: "test-key",
    baseUrl: "https://api.psecs.io",
  };

  it("registers without throwing", () => {
    const server = new McpServer({ name: "test", version: "0.0.1" });
    const client = new PsecsClient(config);
    expect(() => registerAccountTools(server, client)).not.toThrow();
  });

  it("can be called multiple times on different servers", () => {
    const client = new PsecsClient(config);
    const server1 = new McpServer({ name: "test1", version: "0.0.1" });
    const server2 = new McpServer({ name: "test2", version: "0.0.1" });
    expect(() => registerAccountTools(server1, client)).not.toThrow();
    expect(() => registerAccountTools(server2, client)).not.toThrow();
  });
});
