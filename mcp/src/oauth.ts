import { createRemoteJWKSet, createLocalJWKSet, jwtVerify, type JWK } from "jose";

export type ProvisionResult =
  | { ok: true; apiKey: string }
  | { ok: false; error: string };

export type TokenResult =
  | { ok: true; userId: string }
  | { ok: false; error: string };

export interface OAuthConfig {
  auth0Domain: string;
  auth0Audience: string;
  serviceSecret: string;
  psecsBaseUrl: string;
  issuerUrl: string;
  jwksUrl: string;
}

const DEFAULT_BASE_URL = "https://api.psecs.io";

export function loadOAuthConfig(): OAuthConfig {
  const auth0Domain = process.env.AUTH0_DOMAIN;
  if (!auth0Domain) {
    throw new Error(
      "AUTH0_DOMAIN environment variable is required in OAuth mode. " +
        "Set it to your Auth0 tenant domain (e.g., myapp.us.auth0.com)."
    );
  }
  if (
    auth0Domain.includes("://") ||
    auth0Domain.includes("/") ||
    auth0Domain.includes("?") ||
    auth0Domain.includes("@")
  ) {
    throw new Error(
      `AUTH0_DOMAIN must be a plain hostname with no protocol or path (e.g., myapp.us.auth0.com). ` +
        `Do not include "https://". Got: "${auth0Domain}"`
    );
  }

  const auth0Audience = process.env.AUTH0_AUDIENCE;
  if (!auth0Audience) {
    throw new Error(
      "AUTH0_AUDIENCE environment variable is required in OAuth mode. " +
        "Set it to the Auth0 API identifier (e.g., https://mcp.psecs.io)."
    );
  }

  const serviceSecret = process.env.MCP_SERVICE_SECRET;
  if (!serviceSecret) {
    throw new Error(
      "MCP_SERVICE_SECRET environment variable is required in OAuth mode. " +
        "This shared secret authenticates the MCP server to the PSECS API."
    );
  }

  const psecsBaseUrl = (process.env.PSECS_BASE_URL ?? DEFAULT_BASE_URL).replace(
    /\/+$/,
    ""
  );

  const issuerUrl = `https://${auth0Domain}/`;
  const jwksUrl = `https://${auth0Domain}/.well-known/jwks.json`;

  return {
    auth0Domain,
    auth0Audience,
    serviceSecret,
    psecsBaseUrl,
    issuerUrl,
    jwksUrl,
  };
}

// Cache the JWKS fetcher per domain (lives for process lifetime)
const jwksCache = new Map<string, ReturnType<typeof createRemoteJWKSet>>();

function getJwks(jwksUrl: string): ReturnType<typeof createRemoteJWKSet> {
  let jwks = jwksCache.get(jwksUrl);
  if (!jwks) {
    jwks = createRemoteJWKSet(new URL(jwksUrl));
    jwksCache.set(jwksUrl, jwks);
  }
  return jwks;
}

/**
 * Validate an Auth0 access token (RS256 JWT).
 *
 * @param token - The raw Bearer token string (without "Bearer " prefix)
 * @param config - OAuth configuration with issuer and audience
 * @param testJwks - Optional JWKS for testing (bypasses remote fetch)
 */
export async function validateAccessToken(
  token: string | undefined,
  config: OAuthConfig,
  testJwks?: { keys: JWK[] }
): Promise<TokenResult> {
  if (!token) {
    return { ok: false, error: "Access token missing" };
  }

  try {
    const keySource = testJwks
      ? createLocalJWKSet(testJwks)
      : getJwks(config.jwksUrl);

    const { payload } = await jwtVerify(token, keySource, {
      issuer: config.issuerUrl,
      audience: config.auth0Audience,
      algorithms: ["RS256"],
    });

    const userId = payload.sub;
    if (!userId) {
      return { ok: false, error: "Token missing sub claim" };
    }

    return { ok: true, userId };
  } catch (err) {
    const message = err instanceof Error ? err.message : "Token validation failed";
    return { ok: false, error: message };
  }
}

/**
 * Call the PSECS API internal endpoint to get or create an API key for a user.
 * Uses the MCP service secret for authentication (not the user's token).
 */
export async function provisionApiKey(
  userId: string,
  config: OAuthConfig
): Promise<ProvisionResult> {
  try {
    const response = await fetch(
      `${config.psecsBaseUrl}/internal/mcp/api-key`,
      {
        method: "POST",
        headers: {
          "X-Service-Key": config.serviceSecret,
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ userId }),
      }
    );

    if (!response.ok) {
      return {
        ok: false,
        error: `API key provisioning failed: HTTP ${response.status}`,
      };
    }

    const body = (await response.json()) as { apiKey?: unknown };
    const apiKey = body.apiKey;
    if (typeof apiKey !== "string" || apiKey.length === 0) {
      return { ok: false, error: "API provisioning returned invalid response: missing apiKey" };
    }
    return { ok: true, apiKey };
  } catch (err) {
    return {
      ok: false,
      error:
        err instanceof Error
          ? err.message
          : "API key provisioning network error",
    };
  }
}
