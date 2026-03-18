import { describe, it, expect, beforeAll, afterAll } from "vitest";
import express from "express";
import request from "supertest";
import type { OAuthProxyConfig } from "./oauth.js";
import { setupOAuthProxy } from "./oauth-proxy.js";
import { JwtIssuer } from "./jwt-issuer.js";
import { OAuthStore } from "./oauth-store.js";
import { PsecsClient } from "./client.js";

/**
 * Build an Express app wired with the OAuth proxy and /mcp endpoint,
 * mirroring the production setup in startHttpOAuth().
 */
function buildOAuthApp(
  config: OAuthProxyConfig,
  store: OAuthStore,
  issuer: JwtIssuer
): ReturnType<typeof express> {
  const app = express();
  app.use(express.json());
  app.use(express.urlencoded({ extended: true }));

  setupOAuthProxy(app, config, store, issuer);

  app.head("/mcp", (_req, res) => {
    res.status(200).end();
  });

  app.all("/mcp", async (req, res) => {
    const authHeader = req.headers.authorization;
    const token =
      authHeader && authHeader.toLowerCase().startsWith("bearer ")
        ? authHeader.slice(7)
        : undefined;

    if (!token) {
      res.setHeader(
        "WWW-Authenticate",
        `Bearer resource_metadata="${config.mcpBaseUrl}/.well-known/oauth-protected-resource", scope="psecs:play"`
      );
      res.status(401).json({ error: "Authentication required" });
      return;
    }

    try {
      const { jwtVerify, createLocalJWKSet } = await import("jose");
      const jwks = issuer.getJwks();
      const keySet = createLocalJWKSet(jwks);
      const { payload } = await jwtVerify(token, keySet, {
        issuer: config.mcpBaseUrl,
        audience: config.mcpBaseUrl,
        algorithms: ["RS256"],
      });

      if (!payload.sub) {
        res.status(401).json({ error: "Token missing sub claim" });
        return;
      }

      const apiKey = await store.getApiKey(payload.sub);
      if (!apiKey) {
        res.status(401).json({ error: "No API key found for user" });
        return;
      }

      // In tests we don't actually call handleMcpRequest; just confirm auth succeeded.
      res.json({ authenticated: true, userId: payload.sub });
    } catch (err) {
      res.setHeader(
        "WWW-Authenticate",
        'Bearer error="invalid_token", error_description="Token validation failed"'
      );
      res.status(401).json({ error: "Token validation failed" });
    }
  });

  app.get("/health", (_req, res) => {
    res.json({ status: "ok", version: "0.0.1", mode: "oauth" });
  });

  return app;
}

describe("OAuth proxy /mcp endpoint", () => {
  const config: OAuthProxyConfig = {
    auth0Domain: "test.us.auth0.com",
    auth0ClientId: "test-client-id",
    auth0ClientSecret: "test-client-secret",
    serviceSecret: "test-secret",
    psecsBaseUrl: "https://api.psecsapi.com",
    auth0IssuerUrl: "https://test.us.auth0.com/",
    mcpBaseUrl: "https://mcp.psecsapi.com",
  };

  let issuer: JwtIssuer;
  let store: OAuthStore;

  beforeAll(async () => {
    issuer = await JwtIssuer.createInMemory();
    store = OAuthStore.createInMemory();
  });

  afterAll(() => {
    store.destroy();
  });

  it("returns 401 with resource_metadata when no token is provided", async () => {
    const app = buildOAuthApp(config, store, issuer);

    const res = await request(app).post("/mcp").send({});

    expect(res.status).toBe(401);
    expect(res.headers["www-authenticate"]).toContain("resource_metadata");
    expect(res.headers["www-authenticate"]).toContain("psecs:play");
    expect(res.body.error).toBe("Authentication required");
  });

  it("returns 401 with invalid_token for a bogus JWT", async () => {
    const app = buildOAuthApp(config, store, issuer);

    const res = await request(app)
      .post("/mcp")
      .set("Authorization", "Bearer not.a.real.jwt")
      .send({});

    expect(res.status).toBe(401);
    expect(res.headers["www-authenticate"]).toBe(
      'Bearer error="invalid_token", error_description="Token validation failed"'
    );
    expect(res.body.error).toBe("Token validation failed");
  });

  it("returns 401 when JWT is valid but no API key is in store", async () => {
    const app = buildOAuthApp(config, store, issuer);

    const token = await issuer.sign({
      sub: "auth0|user-no-key",
      scope: "psecs:play",
    });

    const res = await request(app)
      .post("/mcp")
      .set("Authorization", `Bearer ${token}`)
      .send({});

    expect(res.status).toBe(401);
    expect(res.body.error).toBe("No API key found for user");
  });

  it("authenticates successfully with valid JWT and stored API key", async () => {
    await store.setApiKey("auth0|test-user", "psecs_sk_test_key");
    const app = buildOAuthApp(config, store, issuer);

    const token = await issuer.sign({
      sub: "auth0|test-user",
      scope: "psecs:play",
    });

    const res = await request(app)
      .post("/mcp")
      .set("Authorization", `Bearer ${token}`)
      .send({});

    expect(res.status).toBe(200);
    expect(res.body.authenticated).toBe(true);
    expect(res.body.userId).toBe("auth0|test-user");
  });

  it('accepts UPPER CASE "BEARER" scheme (RFC 6750 case-insensitive)', async () => {
    await store.setApiKey("auth0|upper-user", "psecs_sk_upper_key");
    const app = buildOAuthApp(config, store, issuer);

    const token = await issuer.sign({
      sub: "auth0|upper-user",
      scope: "psecs:play",
    });

    const res = await request(app)
      .post("/mcp")
      .set("Authorization", `BEARER ${token}`)
      .send({});

    expect(res.status).toBe(200);
    expect(res.body.authenticated).toBe(true);
  });

  it("rejects a JWT signed by a different key", async () => {
    const otherIssuer = await JwtIssuer.createInMemory();
    const app = buildOAuthApp(config, store, issuer);

    const token = await otherIssuer.sign({
      sub: "auth0|test-user",
      scope: "psecs:play",
    });

    const res = await request(app)
      .post("/mcp")
      .set("Authorization", `Bearer ${token}`)
      .send({});

    expect(res.status).toBe(401);
    expect(res.body.error).toBe("Token validation failed");
  });

  it("serves GET /health with oauth mode indicator", async () => {
    const app = buildOAuthApp(config, store, issuer);

    const res = await request(app).get("/health");

    expect(res.status).toBe(200);
    expect(res.body.status).toBe("ok");
    expect(res.body.mode).toBe("oauth");
  });

  it("responds 200 to HEAD /mcp", async () => {
    const app = buildOAuthApp(config, store, issuer);

    const res = await request(app).head("/mcp");

    expect(res.status).toBe(200);
  });
});
