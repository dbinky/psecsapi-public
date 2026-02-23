import { describe, it, expect } from "vitest";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { PsecsClient } from "../client.js";
import { registerPrompts } from "./game-guide.js";

describe("game guide prompt", () => {
  it("registers prompt on the server", () => {
    const server = new McpServer({ name: "test", version: "0.0.1" });
    const client = new PsecsClient({
      apiKey: "psecs_sk_test",
      baseUrl: "http://localhost:5130",
    });
    expect(() => registerPrompts(server, client)).not.toThrow();
  });

  it("can be called multiple times on different servers", () => {
    const client = new PsecsClient({
      apiKey: "psecs_sk_test",
      baseUrl: "http://localhost:5130",
    });
    const server1 = new McpServer({ name: "test1", version: "0.0.1" });
    const server2 = new McpServer({ name: "test2", version: "0.0.1" });
    expect(() => registerPrompts(server1, client)).not.toThrow();
    expect(() => registerPrompts(server2, client)).not.toThrow();
  });
});
