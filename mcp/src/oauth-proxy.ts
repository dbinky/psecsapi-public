import crypto from "node:crypto";
import express from "express";
import type { OAuthProxyConfig } from "./oauth.js";
import { exchangeAuth0Code, provisionApiKey } from "./oauth.js";
import type { OAuthStore } from "./oauth-store.js";
import type { JwtIssuer } from "./jwt-issuer.js";

export function setupOAuthProxy(
  app: ReturnType<typeof express>,
  config: OAuthProxyConfig,
  store: OAuthStore,
  issuer: JwtIssuer
): void {
  // --- Discovery endpoints ---
  // Serve at both root and path-aware URLs per RFC 9728 Section 3.1.
  // When the MCP server URL is https://host/mcp, clients may derive the metadata
  // URL as /.well-known/oauth-protected-resource/mcp (path-aware) or just
  // /.well-known/oauth-protected-resource (root). Support both.

  const protectedResourceMetadata = {
    resource: config.mcpBaseUrl,
    authorization_servers: [config.mcpBaseUrl],
    scopes_supported: ["psecs:play"],
    bearer_methods_supported: ["header"],
  };

  const authServerMetadata = {
    issuer: config.mcpBaseUrl,
    authorization_endpoint: `${config.mcpBaseUrl}/oauth/authorize`,
    token_endpoint: `${config.mcpBaseUrl}/oauth/token`,
    registration_endpoint: `${config.mcpBaseUrl}/oauth/register`,
    jwks_uri: `${config.mcpBaseUrl}/.well-known/jwks.json`,
    response_types_supported: ["code"],
    grant_types_supported: ["authorization_code", "refresh_token"],
    code_challenge_methods_supported: ["S256"],
    scopes_supported: ["psecs:play"],
    token_endpoint_auth_methods_supported: ["none"],
  };

  app.get("/.well-known/oauth-protected-resource", (_req, res) => {
    res.json(protectedResourceMetadata);
  });
  app.get("/.well-known/oauth-protected-resource/*", (_req, res) => {
    res.json(protectedResourceMetadata);
  });

  app.get("/.well-known/oauth-authorization-server", (_req, res) => {
    res.json(authServerMetadata);
  });
  app.get("/.well-known/oauth-authorization-server/*", (_req, res) => {
    res.json(authServerMetadata);
  });

  app.get("/.well-known/jwks.json", (_req, res) => {
    res.json(issuer.getJwks());
  });

  // --- Dynamic Client Registration ---

  app.post("/oauth/register", express.json(), (req, res) => {
    const { client_name, redirect_uris, token_endpoint_auth_method } = req.body ?? {};

    if (!Array.isArray(redirect_uris) || redirect_uris.length === 0) {
      res.status(400).json({ error: "redirect_uris must be a non-empty array" });
      return;
    }

    for (const uri of redirect_uris) {
      if (typeof uri !== "string") {
        res.status(400).json({ error: "Each redirect_uri must be a string" });
        return;
      }
      const isLocalhost = uri.startsWith("http://localhost");
      const isHttps = uri.startsWith("https://");
      if (!isHttps && !isLocalhost) {
        res.status(400).json({ error: `Invalid redirect_uri: ${uri}. Only https:// URIs are accepted (http://localhost allowed for dev).` });
        return;
      }
    }

    const clientId = store.registerClient({
      clientName: client_name ?? "unnamed",
      redirectUris: redirect_uris,
    });

    res.status(201).json({
      client_id: clientId,
      client_name: client_name ?? "unnamed",
      redirect_uris,
      token_endpoint_auth_method: "none",
    });
  });

  // --- Authorize ---

  app.get("/oauth/authorize", (req, res) => {
    const {
      response_type,
      client_id,
      redirect_uri,
      code_challenge,
      code_challenge_method,
      state,
      scope,
    } = req.query as Record<string, string>;

    if (response_type !== "code") {
      res.status(400).json({ error: "response_type must be 'code'" });
      return;
    }

    if (!code_challenge) {
      res.status(400).json({ error: "code_challenge is required" });
      return;
    }

    if (code_challenge_method !== "S256") {
      res.status(400).json({ error: "code_challenge_method must be 'S256'" });
      return;
    }

    const client = store.getClient(client_id);
    if (!client) {
      res.status(400).json({ error: "Unknown client_id" });
      return;
    }

    if (!client.redirectUris.includes(redirect_uri)) {
      res.status(400).json({ error: "redirect_uri does not match any registered URI" });
      return;
    }

    const csrfToken = crypto.randomBytes(16).toString("base64url");

    const sessionId = store.createAuthSession({
      clientId: client_id,
      redirectUri: redirect_uri,
      codeChallenge: code_challenge,
      clientState: state ?? "",
      scope: scope ?? "psecs:play",
      csrfToken,
    });

    const compoundState = Buffer.from(
      JSON.stringify({ sid: sessionId, csrf: csrfToken })
    ).toString("base64url");

    const auth0Url = new URL(`https://${config.auth0Domain}/authorize`);
    auth0Url.searchParams.set("response_type", "code");
    auth0Url.searchParams.set("client_id", config.auth0ClientId);
    auth0Url.searchParams.set("redirect_uri", `${config.mcpBaseUrl}/oauth/callback`);
    auth0Url.searchParams.set("state", compoundState);
    auth0Url.searchParams.set("scope", "openid");

    res.redirect(auth0Url.toString());
  });

  // --- Callback ---

  app.get("/oauth/callback", async (req, res) => {
    try {
      const { code, state } = req.query as Record<string, string>;

      if (!code || !state) {
        res.status(400).json({ error: "Missing code or state parameter" });
        return;
      }

      let sid: string;
      let csrf: string;
      try {
        const decoded = JSON.parse(Buffer.from(state, "base64url").toString());
        sid = decoded.sid;
        csrf = decoded.csrf;
      } catch {
        res.status(400).json({ error: "Invalid state parameter" });
        return;
      }

      const session = store.consumeAuthSession(sid);
      if (!session) {
        res.status(400).json({ error: "Invalid or expired auth session" });
        return;
      }

      if (session.csrfToken !== csrf) {
        res.status(400).json({ error: "CSRF token mismatch" });
        return;
      }

      const exchangeResult = await exchangeAuth0Code(code, config);
      if (!exchangeResult.ok) {
        res.status(502).json({ error: exchangeResult.error });
        return;
      }
      const { userId } = exchangeResult;

      let apiKey = await store.getApiKey(userId);
      if (!apiKey) {
        const provisionResult = await provisionApiKey(userId, config);
        if (!provisionResult.ok) {
          res.status(502).json({ error: provisionResult.error });
          return;
        }
        apiKey = provisionResult.apiKey;
        await store.setApiKey(userId, apiKey);
      }

      const authCode = store.createAuthCode({
        clientId: session.clientId,
        userId,
        apiKey,
        codeChallenge: session.codeChallenge,
        redirectUri: session.redirectUri,
        clientState: session.clientState,
        scope: session.scope,
      });

      const redirectUrl = new URL(session.redirectUri);
      redirectUrl.searchParams.set("code", authCode);
      if (session.clientState) {
        redirectUrl.searchParams.set("state", session.clientState);
      }

      res.redirect(redirectUrl.toString());
    } catch (err) {
      console.error("[psecs-mcp] OAuth callback error:", err);
      res.status(500).json({ error: "Internal server error" });
    }
  });

  // --- Token ---

  app.post("/oauth/token", express.urlencoded({ extended: true }), async (req, res) => {
    try {
      const { grant_type } = req.body;

      if (grant_type === "authorization_code") {
        await handleAuthorizationCodeGrant(req.body, config, store, issuer, res);
      } else if (grant_type === "refresh_token") {
        await handleRefreshTokenGrant(req.body, store, issuer, res);
      } else {
        res.status(400).json({ error: "Unsupported grant_type" });
      }
    } catch (err) {
      console.error("[psecs-mcp] Token endpoint error:", err);
      res.status(500).json({ error: "Internal server error" });
    }
  });
}

async function handleAuthorizationCodeGrant(
  body: Record<string, string>,
  config: OAuthProxyConfig,
  store: OAuthStore,
  issuer: JwtIssuer,
  res: express.Response
): Promise<void> {
  const { code, code_verifier, client_id, redirect_uri } = body;

  const data = store.consumeAuthCode(code);
  if (!data) {
    res.status(400).json({ error: "Invalid or expired authorization code" });
    return;
  }

  if (data.clientId !== client_id) {
    res.status(400).json({ error: "client_id mismatch" });
    return;
  }

  if (data.redirectUri !== redirect_uri) {
    res.status(400).json({ error: "redirect_uri mismatch" });
    return;
  }

  // PKCE verification
  const computedChallenge = crypto
    .createHash("sha256")
    .update(code_verifier)
    .digest("base64url");

  if (computedChallenge !== data.codeChallenge) {
    res.status(400).json({ error: "PKCE code_verifier validation failed" });
    return;
  }

  const accessToken = await issuer.sign({
    sub: data.userId,
    scope: data.scope,
  });

  const refreshToken = await store.createRefreshToken({
    userId: data.userId,
    clientId: data.clientId,
    apiKey: data.apiKey,
  });

  await store.setApiKey(data.userId, data.apiKey);

  res.json({
    access_token: accessToken,
    token_type: "Bearer",
    expires_in: 3600,
    refresh_token: refreshToken,
    scope: data.scope,
  });
}

async function handleRefreshTokenGrant(
  body: Record<string, string>,
  store: OAuthStore,
  issuer: JwtIssuer,
  res: express.Response
): Promise<void> {
  const { refresh_token } = body;

  const data = await store.consumeRefreshToken(refresh_token);
  if (!data) {
    res.status(400).json({ error: "Invalid or expired refresh token" });
    return;
  }

  const accessToken = await issuer.sign({
    sub: data.userId,
    scope: "psecs:play",
  });

  const newRefreshToken = await store.createRefreshToken(data);

  res.json({
    access_token: accessToken,
    token_type: "Bearer",
    expires_in: 3600,
    refresh_token: newRefreshToken,
    scope: "psecs:play",
  });
}
