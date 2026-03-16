import { describe, it, expect, vi } from "vitest";
import express from "express";
import request from "supertest";
import { type OAuthConfig } from "./oauth.js";
import { setupOAuthRoutes } from "./server.js";

// Mock the oauth module so we can control validateAccessToken behavior
// without network calls or real JWT keys.
vi.mock("./oauth.js", async (importOriginal) => {
  const actual = await importOriginal<typeof import("./oauth.js")>();
  return {
    ...actual,
    validateAccessToken: vi.fn(),
    provisionApiKey: vi.fn(),
  };
});

describe("setupOAuthRoutes", () => {
  const config: OAuthConfig = {
    auth0Domain: "test.us.auth0.com",
    auth0Audience: "https://mcp.psecs.io",
    serviceSecret: "test-secret",
    psecsBaseUrl: "https://api.psecs.io",
    issuerUrl: "https://test.us.auth0.com/",
    jwksUrl: "https://test.us.auth0.com/.well-known/jwks.json",
  };

  it('uses generic error in WWW-Authenticate, not raw jose error with double-quotes', async () => {
    // Simulate a jose error message that contains double-quotes —
    // the kind produced by: JWT "iss" claim check failed, JWT "aud" claim check failed, etc.
    const { validateAccessToken } = await import("./oauth.js");
    vi.mocked(validateAccessToken).mockResolvedValueOnce({
      ok: false,
      error: 'JWT "iss" claim check failed',
    });

    const app = express();
    app.use(express.json());
    setupOAuthRoutes(app, config, new Map());

    const res = await request(app)
      .post("/mcp")
      .set("Authorization", "Bearer some-token-that-fails-validation")
      .send({});

    expect(res.status).toBe(401);
    // The header must NOT contain the raw jose error (which has unescaped double-quotes)
    expect(res.headers["www-authenticate"]).not.toContain('"iss"');
    // The header must use the safe, generic message
    expect(res.headers["www-authenticate"]).toBe(
      'Bearer error="invalid_token", error_description="Token validation failed"'
    );
    expect(res.body.error).toBe("Token validation failed");
  });

  it('returns 401 with resource_metadata when no token is provided', async () => {
    const app = express();
    app.use(express.json());
    setupOAuthRoutes(app, config, new Map());

    const res = await request(app).post("/mcp").send({});

    expect(res.status).toBe(401);
    expect(res.headers["www-authenticate"]).toContain("resource_metadata");
    expect(res.headers["www-authenticate"]).toContain("psecs:play");
  });

  it('serves GET /health with oauth mode indicator', async () => {
    // The health endpoint is unauthenticated and used by Azure Container Apps
    // health monitoring. If it were accidentally removed from setupOAuthRoutes,
    // the container would fail health checks and the deployment would alert.
    const app = express();
    app.use(express.json());
    setupOAuthRoutes(app, config, new Map());

    const res = await request(app).get("/health");

    expect(res.status).toBe(200);
    expect(res.body.status).toBe("ok");
    expect(res.body.mode).toBe("oauth");
  });

  it('accepts UPPER CASE "BEARER" scheme (RFC 6750 case-insensitive)', async () => {
    // RFC 6750 / RFC 9110: auth-scheme is case-insensitive; complements the
    // existing lowercase "bearer" test. "BEARER" must also extract the token.
    const { validateAccessToken } = await import("./oauth.js");
    vi.mocked(validateAccessToken).mockResolvedValueOnce({
      ok: false,
      error: "test rejection — only verifying token extraction",
    });

    const app = express();
    app.use(express.json());
    setupOAuthRoutes(app, config, new Map());

    await request(app)
      .post("/mcp")
      .set("Authorization", "BEARER upper-case-token")
      .send({});

    expect(vi.mocked(validateAccessToken)).toHaveBeenCalledWith(
      "upper-case-token",
      expect.anything()
    );
  });

  it('skips provisionApiKey when userId is already in the cache', async () => {
    // If the cache has the user's API key, provisionApiKey must not be called.
    // This is the core correctness property of the caching layer — without it,
    // every request would re-provision, generating a new key and invalidating
    // the prior one on the PSECS side.
    const { validateAccessToken, provisionApiKey } = await import("./oauth.js");
    vi.mocked(validateAccessToken).mockResolvedValueOnce({
      ok: true,
      userId: "auth0|cached-user",
    });

    const prePopulatedCache = new Map([["auth0|cached-user", "psecs_sk_cached_key"]]);

    const app = express();
    app.use(express.json());
    setupOAuthRoutes(app, config, prePopulatedCache);

    await request(app)
      .post("/mcp")
      .set("Authorization", "Bearer some-valid-token")
      .send({});

    expect(vi.mocked(provisionApiKey)).not.toHaveBeenCalled();
  });

  it('populates cache after successful provision so second request skips provisionApiKey', async () => {
    // Verifies that apiKeyCache.set() is called after a successful provision.
    // Without this, every request would re-provision and generate a new API key
    // (invalidating the prior one), which would break any ongoing session.
    const { validateAccessToken, provisionApiKey } = await import("./oauth.js");
    vi.mocked(validateAccessToken).mockResolvedValue({
      ok: true,
      userId: "auth0|new-user",
    });
    vi.mocked(provisionApiKey).mockResolvedValueOnce({
      ok: true,
      apiKey: "psecs_sk_fresh_key",
    });
    // provisionApiKey only mocked for one call — a second call would return undefined,
    // failing the test if cache were not populated after the first successful provision.

    const sharedCache = new Map<string, string>();
    const app = express();
    app.use(express.json());
    setupOAuthRoutes(app, config, sharedCache);

    // First request: triggers provision
    await request(app)
      .post("/mcp")
      .set("Authorization", "Bearer valid-token")
      .send({});

    // Second request: must use the cached key, not call provisionApiKey again
    await request(app)
      .post("/mcp")
      .set("Authorization", "Bearer valid-token")
      .send({});

    expect(vi.mocked(provisionApiKey)).toHaveBeenCalledTimes(1);
    expect(sharedCache.get("auth0|new-user")).toBe("psecs_sk_fresh_key");
  });

  it('returns 502 when API key provisioning fails after valid token', async () => {
    // A valid token that passes JWT validation but whose user cannot have an API key
    // provisioned (e.g., PSECS API unreachable) must produce a clean 502 response.
    // Without this test, a broken error path could cause a 500 or hang for MCP clients.
    const { validateAccessToken, provisionApiKey } = await import("./oauth.js");
    vi.mocked(validateAccessToken).mockResolvedValueOnce({
      ok: true,
      userId: "auth0|user-provision-fails",
    });
    vi.mocked(provisionApiKey).mockResolvedValueOnce({
      ok: false,
      error: "PSECS API unreachable",
    });

    const app = express();
    app.use(express.json());
    setupOAuthRoutes(app, config, new Map());

    const res = await request(app)
      .post("/mcp")
      .set("Authorization", "Bearer valid-token")
      .send({});

    expect(res.status).toBe(502);
    expect(res.body.error).toContain("provision");
  });

  it('accepts lowercase "bearer" scheme (RFC 6750 case-insensitive)', async () => {
    // RFC 6750 / RFC 9110: auth-scheme identifiers are case-insensitive.
    // Some OAuth clients send "bearer" (lowercase) instead of "Bearer".
    // Verify the token is extracted and passed to validateAccessToken — if the prefix
    // check were case-sensitive, validateAccessToken would never be called and the
    // response would be 401 with "Authentication required".
    const { validateAccessToken } = await import("./oauth.js");
    vi.mocked(validateAccessToken).mockResolvedValueOnce({
      ok: false,
      error: "test rejection — we only care that extraction succeeded",
    });

    const app = express();
    app.use(express.json());
    setupOAuthRoutes(app, config, new Map());

    await request(app)
      .post("/mcp")
      .set("Authorization", "bearer lowercase-token")
      .send({});

    // validateAccessToken must have been called with the extracted token,
    // proving "bearer" (lowercase) was accepted as the auth scheme.
    expect(vi.mocked(validateAccessToken)).toHaveBeenCalledWith(
      "lowercase-token",
      expect.anything()
    );
  });
});
