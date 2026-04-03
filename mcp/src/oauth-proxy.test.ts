import { describe, it, expect, beforeAll, afterAll, vi, beforeEach } from "vitest";
import express from "express";
import request from "supertest";
import crypto from "node:crypto";
import { JwtIssuer } from "./jwt-issuer.js";
import { OAuthStore } from "./oauth-store.js";
import { setupOAuthProxy } from "./oauth-proxy.js";
import type { OAuthProxyConfig } from "./oauth.js";
import { jwtVerify, createLocalJWKSet } from "jose";

vi.mock("./oauth.js", async (importOriginal) => {
  const actual = await importOriginal<typeof import("./oauth.js")>();
  return {
    ...actual,
    exchangeAuth0Code: vi.fn(),
    provisionApiKey: vi.fn(),
  };
});

import { exchangeAuth0Code, provisionApiKey } from "./oauth.js";

const mockedExchangeAuth0Code = vi.mocked(exchangeAuth0Code);
const mockedProvisionApiKey = vi.mocked(provisionApiKey);

function createS256Challenge(verifier: string): string {
  return crypto.createHash("sha256").update(verifier).digest("base64url");
}

function makeConfig(overrides?: Partial<OAuthProxyConfig>): OAuthProxyConfig {
  return {
    auth0Domain: "test.us.auth0.com",
    auth0ClientId: "auth0-client-id",
    auth0ClientSecret: "auth0-client-secret",
    serviceSecret: "service-secret",
    psecsBaseUrl: "https://api.psecsapi.com",
    auth0IssuerUrl: "https://test.us.auth0.com/",
    mcpBaseUrl: "https://mcp.psecsapi.com",
    ...overrides,
  };
}

function createApp(config: OAuthProxyConfig, store: OAuthStore, issuer: JwtIssuer) {
  const app = express();
  setupOAuthProxy(app, config, store, issuer);
  return app;
}

describe("OAuth proxy routes", () => {
  let store: OAuthStore;
  let issuer: JwtIssuer;
  let config: OAuthProxyConfig;
  let app: ReturnType<typeof express>;

  beforeAll(async () => {
    issuer = await JwtIssuer.createInMemory();
  });

  beforeEach(() => {
    store = OAuthStore.createInMemory();
    config = makeConfig();
    app = createApp(config, store, issuer);
    vi.clearAllMocks();
  });

  afterAll(() => {
    store?.destroy();
  });

  // --- Discovery endpoints ---

  describe("GET /.well-known/oauth-protected-resource", () => {
    it("returns correct metadata", async () => {
      const res = await request(app).get("/.well-known/oauth-protected-resource");
      expect(res.status).toBe(200);
      expect(res.body).toEqual({
        resource: "https://mcp.psecsapi.com",
        authorization_servers: ["https://mcp.psecsapi.com"],
        scopes_supported: ["psecs:play"],
        bearer_methods_supported: ["header"],
      });
    });
  });

  describe("GET /.well-known/oauth-authorization-server", () => {
    it("returns RFC 8414 metadata", async () => {
      const res = await request(app).get("/.well-known/oauth-authorization-server");
      expect(res.status).toBe(200);
      expect(res.body).toEqual({
        issuer: "https://mcp.psecsapi.com",
        authorization_endpoint: "https://mcp.psecsapi.com/oauth/authorize",
        token_endpoint: "https://mcp.psecsapi.com/oauth/token",
        registration_endpoint: "https://mcp.psecsapi.com/oauth/register",
        jwks_uri: "https://mcp.psecsapi.com/.well-known/jwks.json",
        response_types_supported: ["code"],
        grant_types_supported: ["authorization_code", "refresh_token"],
        code_challenge_methods_supported: ["S256"],
        scopes_supported: ["psecs:play"],
        token_endpoint_auth_methods_supported: ["none"],
      });
    });
  });

  describe("GET /.well-known/jwks.json", () => {
    it("returns public key without private key material", async () => {
      const res = await request(app).get("/.well-known/jwks.json");
      expect(res.status).toBe(200);
      expect(res.body.keys).toHaveLength(1);
      expect(res.body.keys[0].kty).toBe("RSA");
      expect(res.body.keys[0]).not.toHaveProperty("d");
      expect(res.body.keys[0]).not.toHaveProperty("p");
      expect(res.body.keys[0]).not.toHaveProperty("q");
    });
  });

  // --- DCR ---

  describe("POST /oauth/register", () => {
    it("returns client_id for valid https redirect", async () => {
      const res = await request(app)
        .post("/oauth/register")
        .send({
          client_name: "TestClient",
          redirect_uris: ["https://example.com/callback"],
          token_endpoint_auth_method: "none",
        });
      expect(res.status).toBe(201);
      expect(res.body.client_id).toBeDefined();
      expect(res.body.client_name).toBe("TestClient");
      expect(res.body.redirect_uris).toEqual(["https://example.com/callback"]);
      expect(res.body.token_endpoint_auth_method).toBe("none");
      expect(res.body).not.toHaveProperty("client_secret");
    });

    it("rejects http redirect URI", async () => {
      const res = await request(app)
        .post("/oauth/register")
        .send({
          client_name: "TestClient",
          redirect_uris: ["http://example.com/callback"],
        });
      expect(res.status).toBe(400);
      expect(res.body.error).toBeDefined();
    });

    it("accepts http://localhost", async () => {
      const res = await request(app)
        .post("/oauth/register")
        .send({
          client_name: "LocalDev",
          redirect_uris: ["http://localhost:3000/callback"],
        });
      expect(res.status).toBe(201);
      expect(res.body.client_id).toBeDefined();
    });

    it("accepts http://127.0.0.1 (Codex loopback)", async () => {
      const res = await request(app)
        .post("/oauth/register")
        .send({
          client_name: "CodexClient",
          redirect_uris: ["http://127.0.0.1:35547/callback"],
        });
      expect(res.status).toBe(201);
      expect(res.body.client_id).toBeDefined();
    });

    it("rejects empty redirect_uris", async () => {
      const res = await request(app)
        .post("/oauth/register")
        .send({
          client_name: "TestClient",
          redirect_uris: [],
        });
      expect(res.status).toBe(400);
      expect(res.body.error).toBeDefined();
    });
  });

  // --- Authorize ---

  describe("GET /oauth/authorize", () => {
    let clientId: string;

    beforeEach(() => {
      clientId = store.registerClient({
        clientName: "TestClient",
        redirectUris: ["https://example.com/callback"],
      });
    });

    it("redirects to Auth0 with valid params", async () => {
      const res = await request(app)
        .get("/oauth/authorize")
        .query({
          response_type: "code",
          client_id: clientId,
          redirect_uri: "https://example.com/callback",
          code_challenge: "test-challenge",
          code_challenge_method: "S256",
          state: "client-state-123",
          scope: "psecs:play",
        });

      expect(res.status).toBe(302);
      const location = new URL(res.headers.location);
      expect(location.hostname).toBe("test.us.auth0.com");
      expect(location.pathname).toBe("/authorize");
      expect(location.searchParams.get("response_type")).toBe("code");
      expect(location.searchParams.get("client_id")).toBe("auth0-client-id");
      expect(location.searchParams.get("redirect_uri")).toBe("https://mcp.psecsapi.com/oauth/callback");
      expect(location.searchParams.get("scope")).toBe("openid");
      // Compound state should be base64url encoded JSON
      const stateParam = location.searchParams.get("state")!;
      const decoded = JSON.parse(Buffer.from(stateParam, "base64url").toString());
      expect(decoded.sid).toBeDefined();
      expect(decoded.csrf).toBeDefined();
    });

    it("rejects unknown client_id", async () => {
      const res = await request(app)
        .get("/oauth/authorize")
        .query({
          response_type: "code",
          client_id: "unknown-client",
          redirect_uri: "https://example.com/callback",
          code_challenge: "test-challenge",
          code_challenge_method: "S256",
          state: "s",
        });
      expect(res.status).toBe(400);
      expect(res.body.error).toBeDefined();
    });

    it("rejects mismatched redirect_uri", async () => {
      const res = await request(app)
        .get("/oauth/authorize")
        .query({
          response_type: "code",
          client_id: clientId,
          redirect_uri: "https://evil.com/callback",
          code_challenge: "test-challenge",
          code_challenge_method: "S256",
          state: "s",
        });
      expect(res.status).toBe(400);
      expect(res.body.error).toBeDefined();
    });

    it("rejects missing code_challenge", async () => {
      const res = await request(app)
        .get("/oauth/authorize")
        .query({
          response_type: "code",
          client_id: clientId,
          redirect_uri: "https://example.com/callback",
          code_challenge_method: "S256",
          state: "s",
        });
      expect(res.status).toBe(400);
      expect(res.body.error).toBeDefined();
    });

    it("rejects response_type other than code", async () => {
      const res = await request(app)
        .get("/oauth/authorize")
        .query({
          response_type: "token",
          client_id: clientId,
          redirect_uri: "https://example.com/callback",
          code_challenge: "test-challenge",
          code_challenge_method: "S256",
          state: "s",
        });
      expect(res.status).toBe(400);
      expect(res.body.error).toBeDefined();
    });
  });

  // --- Token (authorization_code) ---

  describe("POST /oauth/token (authorization_code)", () => {
    const verifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";
    const challenge = createS256Challenge(verifier);
    const userId = "auth0|user-123";
    const apiKey = "psecs-api-key-abc";
    let clientId: string;

    beforeEach(() => {
      clientId = store.registerClient({
        clientName: "TestClient",
        redirectUris: ["https://example.com/callback"],
      });
    });

    it("issues JWT for valid code + PKCE verifier", async () => {
      const code = store.createAuthCode({
        clientId,
        userId,
        apiKey,
        codeChallenge: challenge,
        redirectUri: "https://example.com/callback",
        clientState: "s",
        scope: "psecs:play",
      });

      const res = await request(app)
        .post("/oauth/token")
        .type("form")
        .send({
          grant_type: "authorization_code",
          code,
          code_verifier: verifier,
          client_id: clientId,
          redirect_uri: "https://example.com/callback",
        });

      expect(res.status).toBe(200);
      expect(res.body.token_type).toBe("Bearer");
      expect(res.body.expires_in).toBe(3600);
      expect(res.body.access_token).toBeDefined();
      expect(res.body.refresh_token).toBeDefined();
      expect(res.body.scope).toBe("psecs:play");

      // Verify the JWT is valid
      const jwks = issuer.getJwks();
      const keySet = createLocalJWKSet(jwks);
      const { payload } = await jwtVerify(res.body.access_token, keySet);
      expect(payload.sub).toBe(userId);
      expect(payload.scope).toBe("psecs:play");
    });

    it("rejects wrong code_verifier", async () => {
      const code = store.createAuthCode({
        clientId,
        userId,
        apiKey,
        codeChallenge: challenge,
        redirectUri: "https://example.com/callback",
        clientState: "s",
        scope: "psecs:play",
      });

      const res = await request(app)
        .post("/oauth/token")
        .type("form")
        .send({
          grant_type: "authorization_code",
          code,
          code_verifier: "wrong-verifier",
          client_id: clientId,
          redirect_uri: "https://example.com/callback",
        });

      expect(res.status).toBe(400);
      expect(res.body.error).toBeDefined();
    });

    it("rejects replayed authorization code", async () => {
      const code = store.createAuthCode({
        clientId,
        userId,
        apiKey,
        codeChallenge: challenge,
        redirectUri: "https://example.com/callback",
        clientState: "s",
        scope: "psecs:play",
      });

      // First use succeeds
      await request(app)
        .post("/oauth/token")
        .type("form")
        .send({
          grant_type: "authorization_code",
          code,
          code_verifier: verifier,
          client_id: clientId,
          redirect_uri: "https://example.com/callback",
        });

      // Second use fails
      const res = await request(app)
        .post("/oauth/token")
        .type("form")
        .send({
          grant_type: "authorization_code",
          code,
          code_verifier: verifier,
          client_id: clientId,
          redirect_uri: "https://example.com/callback",
        });

      expect(res.status).toBe(400);
      expect(res.body.error).toBeDefined();
    });
  });

  // --- Token (refresh_token) ---

  describe("POST /oauth/token (refresh_token)", () => {
    let clientId: string;

    beforeEach(() => {
      clientId = store.registerClient({
        clientName: "TestClient",
        redirectUris: ["https://example.com/callback"],
      });
    });

    it("returns new JWT + rotated refresh token", async () => {
      const refreshToken = await store.createRefreshToken({
        userId: "auth0|user-123",
        clientId,
        apiKey: "psecs-api-key-abc",
      });

      const res = await request(app)
        .post("/oauth/token")
        .type("form")
        .send({
          grant_type: "refresh_token",
          refresh_token: refreshToken,
          client_id: clientId,
        });

      expect(res.status).toBe(200);
      expect(res.body.token_type).toBe("Bearer");
      expect(res.body.expires_in).toBe(3600);
      expect(res.body.access_token).toBeDefined();
      expect(res.body.refresh_token).toBeDefined();
      expect(res.body.scope).toBe("psecs:play");

      // New refresh token should be different from original
      expect(res.body.refresh_token).not.toBe(refreshToken);

      // Verify the JWT
      const jwks = issuer.getJwks();
      const keySet = createLocalJWKSet(jwks);
      const { payload } = await jwtVerify(res.body.access_token, keySet);
      expect(payload.sub).toBe("auth0|user-123");
    });
  });
});
