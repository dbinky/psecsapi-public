import { describe, it, expect, beforeEach, afterEach, vi } from "vitest";
import { loadOAuthConfig, validateAccessToken, provisionApiKey, type OAuthConfig } from "./oauth.js";
import { SignJWT, exportJWK, generateKeyPair } from "jose";

describe("loadOAuthConfig", () => {
  const originalEnv = process.env;

  beforeEach(() => {
    process.env = { ...originalEnv };
  });

  afterEach(() => {
    process.env = originalEnv;
  });

  it("loads config from environment variables", () => {
    process.env.AUTH0_DOMAIN = "test.us.auth0.com";
    process.env.AUTH0_AUDIENCE = "https://mcp.psecs.io";
    process.env.MCP_SERVICE_SECRET = "test-secret-key";
    process.env.PSECS_BASE_URL = "https://api.psecs.io";

    const config = loadOAuthConfig();

    expect(config.auth0Domain).toBe("test.us.auth0.com");
    expect(config.auth0Audience).toBe("https://mcp.psecs.io");
    expect(config.serviceSecret).toBe("test-secret-key");
    expect(config.psecsBaseUrl).toBe("https://api.psecs.io");
    expect(config.issuerUrl).toBe("https://test.us.auth0.com/");
    expect(config.jwksUrl).toBe("https://test.us.auth0.com/.well-known/jwks.json");
  });

  it("throws if AUTH0_DOMAIN is missing", () => {
    process.env.AUTH0_AUDIENCE = "https://mcp.psecs.io";
    process.env.MCP_SERVICE_SECRET = "test-secret-key";
    delete process.env.AUTH0_DOMAIN;

    expect(() => loadOAuthConfig()).toThrow("AUTH0_DOMAIN");
  });

  it("throws if AUTH0_AUDIENCE is missing", () => {
    process.env.AUTH0_DOMAIN = "test.us.auth0.com";
    process.env.MCP_SERVICE_SECRET = "test-secret-key";
    delete process.env.AUTH0_AUDIENCE;

    expect(() => loadOAuthConfig()).toThrow("AUTH0_AUDIENCE");
  });

  it("throws if MCP_SERVICE_SECRET is missing", () => {
    process.env.AUTH0_DOMAIN = "test.us.auth0.com";
    process.env.AUTH0_AUDIENCE = "https://mcp.psecs.io";
    delete process.env.MCP_SERVICE_SECRET;

    expect(() => loadOAuthConfig()).toThrow("MCP_SERVICE_SECRET");
  });

  it("uses PSECS_BASE_URL with default fallback", () => {
    process.env.AUTH0_DOMAIN = "test.us.auth0.com";
    process.env.AUTH0_AUDIENCE = "https://mcp.psecs.io";
    process.env.MCP_SERVICE_SECRET = "test-secret-key";
    delete process.env.PSECS_BASE_URL;

    const config = loadOAuthConfig();
    expect(config.psecsBaseUrl).toBe("https://api.psecs.io");
  });

  it("throws if AUTH0_DOMAIN includes a protocol prefix", () => {
    // Common misconfiguration: operator sets AUTH0_DOMAIN=https://tenant.auth0.com
    // instead of AUTH0_DOMAIN=tenant.auth0.com. Without validation, this silently
    // produces a broken JWKS URL that fails on every JWT validation at runtime.
    process.env.AUTH0_DOMAIN = "https://test.us.auth0.com";
    process.env.AUTH0_AUDIENCE = "https://mcp.psecs.io";
    process.env.MCP_SERVICE_SECRET = "test-secret-key";

    expect(() => loadOAuthConfig()).toThrow("hostname");
  });

  it("throws if AUTH0_DOMAIN contains a path component", () => {
    process.env.AUTH0_DOMAIN = "test.us.auth0.com/extra-path";
    process.env.AUTH0_AUDIENCE = "https://mcp.psecs.io";
    process.env.MCP_SERVICE_SECRET = "test-secret-key";

    expect(() => loadOAuthConfig()).toThrow("hostname");
  });

  it("throws if AUTH0_DOMAIN contains userinfo (@)", () => {
    // Prevent URL manipulation: "attacker@evil.com" → "https://attacker@evil.com/"
    // which would target evil.com for JWKS fetch while appearing to be auth0.com.
    process.env.AUTH0_DOMAIN = "attacker@evil.com";
    process.env.AUTH0_AUDIENCE = "https://mcp.psecs.io";
    process.env.MCP_SERVICE_SECRET = "test-secret-key";

    expect(() => loadOAuthConfig()).toThrow("hostname");
  });

  it("strips multiple trailing slashes from PSECS_BASE_URL", () => {
    process.env.AUTH0_DOMAIN = "test.us.auth0.com";
    process.env.AUTH0_AUDIENCE = "https://mcp.psecs.io";
    process.env.MCP_SERVICE_SECRET = "test-secret-key";
    process.env.PSECS_BASE_URL = "https://api.psecs.io//";

    const config = loadOAuthConfig();
    expect(config.psecsBaseUrl).toBe("https://api.psecs.io");
  });

  it("derives correct issuerUrl and jwksUrl from auth0Domain", () => {
    process.env.AUTH0_DOMAIN = "myapp.us.auth0.com";
    process.env.AUTH0_AUDIENCE = "https://mcp.psecs.io";
    process.env.MCP_SERVICE_SECRET = "test-secret-key";
    delete process.env.PSECS_BASE_URL;

    const config = loadOAuthConfig();
    // Issuer must end with trailing slash (Auth0 convention)
    expect(config.issuerUrl).toBe("https://myapp.us.auth0.com/");
    // JWKS URL must point at the standard Auth0 endpoint
    expect(config.jwksUrl).toBe("https://myapp.us.auth0.com/.well-known/jwks.json");
  });
});

describe("validateAccessToken", () => {
  it("rejects a missing token", async () => {
    const config = makeTestConfig();
    const result = await validateAccessToken(undefined, config);
    expect(result.ok).toBe(false);
    if (!result.ok) expect(result.error).toContain("missing");
  });

  it("rejects a malformed token", async () => {
    const config = makeTestConfig();
    const result = await validateAccessToken("not-a-jwt", config);
    expect(result.ok).toBe(false);
  });

  it("rejects a token with wrong audience", async () => {
    const { privateKey, publicKey } = await generateKeyPair("RS256");
    const jwk = await exportJWK(publicKey);
    jwk.kid = "test-key-id";
    jwk.alg = "RS256";

    const token = await new SignJWT({ sub: "auth0|user123" })
      .setProtectedHeader({ alg: "RS256", kid: "test-key-id" })
      .setIssuer("https://test.us.auth0.com/")
      .setAudience("https://wrong-audience.com")
      .setExpirationTime("1h")
      .sign(privateKey);

    const config = makeTestConfig();
    const result = await validateAccessToken(token, config, { keys: [jwk] });
    expect(result.ok).toBe(false);
  });

  it("rejects a valid token missing the sub claim", async () => {
    // The sub claim identifies the user. A token without it would authenticate
    // successfully (valid signature, issuer, audience, expiry) but return no userId,
    // which must be caught explicitly — jose does not enforce sub presence.
    const { privateKey, publicKey } = await generateKeyPair("RS256");
    const jwk = await exportJWK(publicKey);
    jwk.kid = "test-key-id";
    jwk.alg = "RS256";

    const token = await new SignJWT({})
      .setProtectedHeader({ alg: "RS256", kid: "test-key-id" })
      .setIssuer("https://test.us.auth0.com/")
      .setAudience("https://mcp.psecs.io")
      .setExpirationTime("1h")
      .sign(privateKey);

    const config = makeTestConfig();
    const result = await validateAccessToken(token, config, { keys: [jwk] });
    expect(result.ok).toBe(false);
    if (!result.ok) expect(result.error).toContain("sub");
  });

  it("accepts a valid token and returns user ID", async () => {
    const { privateKey, publicKey } = await generateKeyPair("RS256");
    const jwk = await exportJWK(publicKey);
    jwk.kid = "test-key-id";
    jwk.alg = "RS256";

    const token = await new SignJWT({ sub: "auth0|user123" })
      .setProtectedHeader({ alg: "RS256", kid: "test-key-id" })
      .setIssuer("https://test.us.auth0.com/")
      .setAudience("https://mcp.psecs.io")
      .setExpirationTime("1h")
      .sign(privateKey);

    const config = makeTestConfig();
    const result = await validateAccessToken(token, config, { keys: [jwk] });
    expect(result.ok).toBe(true);
    if (result.ok) expect(result.userId).toBe("auth0|user123");
  });

  it("rejects an expired token", async () => {
    const { privateKey, publicKey } = await generateKeyPair("RS256");
    const jwk = await exportJWK(publicKey);
    jwk.kid = "test-key-id";
    jwk.alg = "RS256";

    const token = await new SignJWT({ sub: "auth0|user123" })
      .setProtectedHeader({ alg: "RS256", kid: "test-key-id" })
      .setIssuer("https://test.us.auth0.com/")
      .setAudience("https://mcp.psecs.io")
      .setExpirationTime("-1h")
      .sign(privateKey);

    const config = makeTestConfig();
    const result = await validateAccessToken(token, config, { keys: [jwk] });
    expect(result.ok).toBe(false);
  });

  it("rejects a token with wrong issuer", async () => {
    const { privateKey, publicKey } = await generateKeyPair("RS256");
    const jwk = await exportJWK(publicKey);
    jwk.kid = "test-key-id";
    jwk.alg = "RS256";

    const token = await new SignJWT({ sub: "auth0|user123" })
      .setProtectedHeader({ alg: "RS256", kid: "test-key-id" })
      .setIssuer("https://wrong-tenant.auth0.com/")
      .setAudience("https://mcp.psecs.io")
      .setExpirationTime("1h")
      .sign(privateKey);

    const config = makeTestConfig();
    const result = await validateAccessToken(token, config, { keys: [jwk] });
    expect(result.ok).toBe(false);
  });

  it("rejects a token signed with HS256 algorithm", async () => {
    // Algorithm-confusion protection: RS256-only enforcement must reject HS256 tokens
    // even when provided with valid public keys. jose error messages for this
    // contain double-quotes (e.g. '"alg" ... value not allowed'), which is why
    // the HTTP layer must not put raw error strings into WWW-Authenticate headers.
    const { privateKey, publicKey } = await generateKeyPair("RS256");
    const jwk = await exportJWK(publicKey);
    jwk.kid = "test-key-id";
    jwk.alg = "RS256";

    // Sign with a symmetric secret (HS256 algorithm) to simulate algorithm confusion
    const secret = new Uint8Array(32).fill(1);
    const token = await new SignJWT({ sub: "auth0|user123" })
      .setProtectedHeader({ alg: "HS256" })
      .setIssuer("https://test.us.auth0.com/")
      .setAudience("https://mcp.psecs.io")
      .setExpirationTime("1h")
      .sign(secret);

    const config = makeTestConfig();
    const result = await validateAccessToken(token, config, { keys: [jwk] });
    expect(result.ok).toBe(false);
  });
});

describe("provisionApiKey", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn());
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("calls the internal endpoint and returns the API key", async () => {
    const mockFetch = vi.mocked(fetch);
    mockFetch.mockResolvedValueOnce(
      new Response(JSON.stringify({ apiKey: "psecs_sk_test123" }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      })
    );

    const config = makeTestConfig();
    const result = await provisionApiKey("auth0|user123", config);

    expect(result.ok).toBe(true);
    if (result.ok) expect(result.apiKey).toBe("psecs_sk_test123");

    expect(mockFetch).toHaveBeenCalledWith(
      "https://api.psecs.io/internal/mcp/api-key",
      expect.objectContaining({
        method: "POST",
        headers: expect.objectContaining({
          "X-Service-Key": "test-secret",
          "Content-Type": "application/json",
        }),
        body: JSON.stringify({ userId: "auth0|user123" }),
      })
    );
  });

  it("returns error on 401 (bad service secret)", async () => {
    const mockFetch = vi.mocked(fetch);
    mockFetch.mockResolvedValueOnce(
      new Response("Unauthorized", { status: 401 })
    );

    const config = makeTestConfig();
    const result = await provisionApiKey("auth0|user123", config);

    expect(result.ok).toBe(false);
    if (!result.ok) expect(result.error).toContain("401");
  });

  it("returns error on network failure", async () => {
    const mockFetch = vi.mocked(fetch);
    mockFetch.mockRejectedValueOnce(new Error("Connection refused"));

    const config = makeTestConfig();
    const result = await provisionApiKey("auth0|user123", config);

    expect(result.ok).toBe(false);
    if (!result.ok) expect(result.error).toContain("Connection refused");
  });

  it("returns error when response body is missing apiKey field", async () => {
    // Guards against server returning 200 with unexpected shape — without this
    // check the function would return { ok: true, apiKey: undefined }, which TypeScript
    // types as string but would silently break all downstream PSECS API calls.
    const mockFetch = vi.mocked(fetch);
    mockFetch.mockResolvedValueOnce(
      new Response(JSON.stringify({}), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      })
    );

    const config = makeTestConfig();
    const result = await provisionApiKey("auth0|user123", config);

    expect(result.ok).toBe(false);
    if (!result.ok) expect(result.error).toContain("apiKey");
  });

  it("returns error when response body has null apiKey", async () => {
    const mockFetch = vi.mocked(fetch);
    mockFetch.mockResolvedValueOnce(
      new Response(JSON.stringify({ apiKey: null }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      })
    );

    const config = makeTestConfig();
    const result = await provisionApiKey("auth0|user123", config);

    expect(result.ok).toBe(false);
  });

  it("returns error when response body has empty apiKey", async () => {
    const mockFetch = vi.mocked(fetch);
    mockFetch.mockResolvedValueOnce(
      new Response(JSON.stringify({ apiKey: "" }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      })
    );

    const config = makeTestConfig();
    const result = await provisionApiKey("auth0|user123", config);

    expect(result.ok).toBe(false);
  });

  it("returns error when response body is not valid JSON", async () => {
    // Defensive: if a proxy returns 200 with an HTML error page, response.json()
    // throws a SyntaxError. The catch block must handle this without crashing.
    const mockFetch = vi.mocked(fetch);
    mockFetch.mockResolvedValueOnce(
      new Response("<html>Error</html>", {
        status: 200,
        headers: { "Content-Type": "text/html" },
      })
    );

    const config = makeTestConfig();
    const result = await provisionApiKey("auth0|user123", config);

    expect(result.ok).toBe(false);
  });
});

function makeTestConfig(): OAuthConfig {
  return {
    auth0Domain: "test.us.auth0.com",
    auth0Audience: "https://mcp.psecs.io",
    serviceSecret: "test-secret",
    psecsBaseUrl: "https://api.psecs.io",
    issuerUrl: "https://test.us.auth0.com/",
    jwksUrl: "https://test.us.auth0.com/.well-known/jwks.json",
  };
}
