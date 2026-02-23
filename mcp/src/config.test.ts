import { describe, it, expect, beforeEach, afterEach } from "vitest";
import { loadConfig } from "./config.js";

describe("loadConfig", () => {
  const originalEnv = { ...process.env };

  beforeEach(() => {
    delete process.env.PSECS_API_KEY;
    delete process.env.PSECS_BASE_URL;
  });

  afterEach(() => {
    process.env = { ...originalEnv };
  });

  it("loads API key from environment", () => {
    process.env.PSECS_API_KEY = "test-key-123";
    const config = loadConfig();
    expect(config.apiKey).toBe("test-key-123");
  });

  it("throws if API key is missing", () => {
    expect(() => loadConfig()).toThrow("PSECS_API_KEY");
  });

  it("uses default base URL when PSECS_BASE_URL is not set", () => {
    process.env.PSECS_API_KEY = "test-key-123";
    const config = loadConfig();
    expect(config.baseUrl).toBe("https://api.psecs.io");
  });

  it("allows custom base URL from environment", () => {
    process.env.PSECS_API_KEY = "test-key-123";
    process.env.PSECS_BASE_URL = "http://localhost:5130";
    const config = loadConfig();
    expect(config.baseUrl).toBe("http://localhost:5130");
  });

  it("strips trailing slash from base URL", () => {
    process.env.PSECS_API_KEY = "test-key-123";
    process.env.PSECS_BASE_URL = "http://localhost:5130/";
    const config = loadConfig();
    expect(config.baseUrl).toBe("http://localhost:5130");
  });

  it("throws if base URL has no http/https scheme", () => {
    process.env.PSECS_API_KEY = "test-key-123";
    process.env.PSECS_BASE_URL = "ftp://example.com";
    expect(() => loadConfig()).toThrow("Must start with http:// or https://");
  });

  it("throws if base URL is a bare hostname", () => {
    process.env.PSECS_API_KEY = "test-key-123";
    process.env.PSECS_BASE_URL = "example.com";
    expect(() => loadConfig()).toThrow("Must start with http:// or https://");
  });
});
