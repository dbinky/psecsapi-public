import { describe, it, expect } from "vitest";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { PsecsClient } from "../client.js";
import { registerGameStateResources } from "./game-state.js";

describe("game state resources", () => {
  it("registers game state resources on the server", () => {
    const server = new McpServer({ name: "test", version: "0.0.1" });
    const client = new PsecsClient({
      apiKey: "psecs_sk_test",
      baseUrl: "http://localhost:5130",
    });
    expect(() => registerGameStateResources(server, client)).not.toThrow();
  });

  it("can be called multiple times on different servers", () => {
    const client = new PsecsClient({
      apiKey: "psecs_sk_test",
      baseUrl: "http://localhost:5130",
    });
    const server1 = new McpServer({ name: "test1", version: "0.0.1" });
    const server2 = new McpServer({ name: "test2", version: "0.0.1" });
    expect(() => registerGameStateResources(server1, client)).not.toThrow();
    expect(() => registerGameStateResources(server2, client)).not.toThrow();
  });
});
