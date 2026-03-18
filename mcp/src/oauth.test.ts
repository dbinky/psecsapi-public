import { describe, it, expect, beforeEach, afterEach, vi } from "vitest";
import { loadOAuthProxyConfig, exchangeAuth0Code, provisionApiKey, type OAuthProxyConfig } from "./oauth.js";

describe("loadOAuthProxyConfig", () => {
  const originalEnv = process.env;
  beforeEach(() => { process.env = { ...originalEnv }; });
  afterEach(() => { process.env = originalEnv; });

  function setAllEnv() {
    process.env.AUTH0_DOMAIN = "test.us.auth0.com";
    process.env.AUTH0_CLIENT_ID = "test-client-id";
    process.env.AUTH0_CLIENT_SECRET = "test-client-secret";
    process.env.MCP_SERVICE_SECRET = "test-service-secret";
    process.env.PSECS_BASE_URL = "https://api.psecsapi.com";
    process.env.MCP_BASE_URL = "https://mcp.psecsapi.com";
  }

  it("loads config from environment variables", () => {
    setAllEnv();
    const config = loadOAuthProxyConfig();
    expect(config.auth0Domain).toBe("test.us.auth0.com");
    expect(config.auth0ClientId).toBe("test-client-id");
    expect(config.auth0ClientSecret).toBe("test-client-secret");
    expect(config.serviceSecret).toBe("test-service-secret");
    expect(config.auth0IssuerUrl).toBe("https://test.us.auth0.com/");
    expect(config.mcpBaseUrl).toBe("https://mcp.psecsapi.com");
  });

  it("throws if AUTH0_DOMAIN is missing", () => {
    setAllEnv();
    delete process.env.AUTH0_DOMAIN;
    expect(() => loadOAuthProxyConfig()).toThrow("AUTH0_DOMAIN");
  });

  it("throws if AUTH0_CLIENT_ID is missing", () => {
    setAllEnv();
    delete process.env.AUTH0_CLIENT_ID;
    expect(() => loadOAuthProxyConfig()).toThrow("AUTH0_CLIENT_ID");
  });

  it("throws if AUTH0_CLIENT_SECRET is missing", () => {
    setAllEnv();
    delete process.env.AUTH0_CLIENT_SECRET;
    expect(() => loadOAuthProxyConfig()).toThrow("AUTH0_CLIENT_SECRET");
  });

  it("throws if AUTH0_DOMAIN has protocol prefix", () => {
    setAllEnv();
    process.env.AUTH0_DOMAIN = "https://test.us.auth0.com";
    expect(() => loadOAuthProxyConfig()).toThrow("hostname");
  });

  it("uses default base URLs when not set", () => {
    setAllEnv();
    delete process.env.PSECS_BASE_URL;
    delete process.env.MCP_BASE_URL;
    const config = loadOAuthProxyConfig();
    expect(config.psecsBaseUrl).toBe("https://api.psecsapi.com");
    expect(config.mcpBaseUrl).toBe("https://mcp.psecsapi.com");
  });

  it("strips trailing slashes from URLs", () => {
    setAllEnv();
    process.env.PSECS_BASE_URL = "https://api.psecsapi.com//";
    process.env.MCP_BASE_URL = "https://mcp.psecsapi.com/";
    const config = loadOAuthProxyConfig();
    expect(config.psecsBaseUrl).toBe("https://api.psecsapi.com");
    expect(config.mcpBaseUrl).toBe("https://mcp.psecsapi.com");
  });
});

describe("exchangeAuth0Code", () => {
  beforeEach(() => { vi.stubGlobal("fetch", vi.fn()); });
  afterEach(() => { vi.restoreAllMocks(); });

  const config = makeTestConfig();

  it("returns userId on success", async () => {
    // Create a fake id_token with sub claim
    const payload = Buffer.from(JSON.stringify({ sub: "auth0|user123" })).toString("base64url");
    const fakeIdToken = `header.${payload}.signature`;

    vi.mocked(fetch).mockResolvedValueOnce(
      new Response(JSON.stringify({ id_token: fakeIdToken }), {
        status: 200, headers: { "Content-Type": "application/json" },
      })
    );

    const result = await exchangeAuth0Code("auth-code-123", config);
    expect(result.ok).toBe(true);
    if (result.ok) expect(result.userId).toBe("auth0|user123");
  });

  it("returns error on non-200", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      new Response("Bad Request", { status: 400 })
    );
    const result = await exchangeAuth0Code("bad-code", config);
    expect(result.ok).toBe(false);
  });

  it("returns error if id_token missing", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      new Response(JSON.stringify({ access_token: "xxx" }), {
        status: 200, headers: { "Content-Type": "application/json" },
      })
    );
    const result = await exchangeAuth0Code("code", config);
    expect(result.ok).toBe(false);
    if (!result.ok) expect(result.error).toContain("id_token");
  });

  it("returns error on network failure", async () => {
    vi.mocked(fetch).mockRejectedValueOnce(new Error("Connection refused"));
    const result = await exchangeAuth0Code("code", config);
    expect(result.ok).toBe(false);
    if (!result.ok) expect(result.error).toContain("Connection refused");
  });
});

describe("provisionApiKey", () => {
  beforeEach(() => { vi.stubGlobal("fetch", vi.fn()); });
  afterEach(() => { vi.restoreAllMocks(); });

  it("calls the internal endpoint and returns the API key", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      new Response(JSON.stringify({ apiKey: "psecs_sk_test123" }), {
        status: 200, headers: { "Content-Type": "application/json" },
      })
    );
    const result = await provisionApiKey("auth0|user123", makeTestConfig());
    expect(result.ok).toBe(true);
    if (result.ok) expect(result.apiKey).toBe("psecs_sk_test123");
  });

  it("returns error on 401", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(new Response("Unauthorized", { status: 401 }));
    const result = await provisionApiKey("auth0|user123", makeTestConfig());
    expect(result.ok).toBe(false);
  });

  it("returns error on network failure", async () => {
    vi.mocked(fetch).mockRejectedValueOnce(new Error("Connection refused"));
    const result = await provisionApiKey("auth0|user123", makeTestConfig());
    expect(result.ok).toBe(false);
  });

  it("returns error when apiKey field missing", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      new Response(JSON.stringify({}), { status: 200, headers: { "Content-Type": "application/json" } })
    );
    const result = await provisionApiKey("auth0|user123", makeTestConfig());
    expect(result.ok).toBe(false);
  });
});

function makeTestConfig(): OAuthProxyConfig {
  return {
    auth0Domain: "test.us.auth0.com",
    auth0ClientId: "test-client-id",
    auth0ClientSecret: "test-client-secret",
    serviceSecret: "test-secret",
    psecsBaseUrl: "https://api.psecsapi.com",
    auth0IssuerUrl: "https://test.us.auth0.com/",
    mcpBaseUrl: "https://mcp.psecsapi.com",
  };
}
