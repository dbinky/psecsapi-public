import { describe, it, expect } from "vitest";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { PsecsClient } from "../client.js";
import { registerGuideResources } from "./guides.js";

describe("guide resources", () => {
  it("registers guide resources on the server", () => {
    const server = new McpServer({ name: "test", version: "0.0.1" });
    const client = new PsecsClient({
      apiKey: "psecs_sk_test",
      baseUrl: "http://localhost:5130",
    });

    expect(() => registerGuideResources(server, client)).not.toThrow();
  });

  it("can be called multiple times on different servers", () => {
    const client = new PsecsClient({
      apiKey: "psecs_sk_test",
      baseUrl: "http://localhost:5130",
    });
    const server1 = new McpServer({ name: "test1", version: "0.0.1" });
    const server2 = new McpServer({ name: "test2", version: "0.0.1" });
    expect(() => registerGuideResources(server1, client)).not.toThrow();
    expect(() => registerGuideResources(server2, client)).not.toThrow();
  });
});
