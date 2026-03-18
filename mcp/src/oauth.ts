export type ProvisionResult =
  | { ok: true; apiKey: string }
  | { ok: false; error: string };

export interface OAuthProxyConfig {
  auth0Domain: string;
  auth0ClientId: string;
  auth0ClientSecret: string;
  serviceSecret: string;
  psecsBaseUrl: string;
  auth0IssuerUrl: string;
  mcpBaseUrl: string;
}

// Note: previous default was "https://api.psecs.io" — updated to match current prod domain
const DEFAULT_PSECS_BASE_URL = "https://api.psecsapi.com";
const DEFAULT_MCP_BASE_URL = "https://mcp.psecsapi.com";

export function loadOAuthProxyConfig(): OAuthProxyConfig {
  const auth0Domain = requireEnv("AUTH0_DOMAIN");
  validateAuth0Domain(auth0Domain);

  return {
    auth0Domain,
    auth0ClientId: requireEnv("AUTH0_CLIENT_ID"),
    auth0ClientSecret: requireEnv("AUTH0_CLIENT_SECRET"),
    serviceSecret: requireEnv("MCP_SERVICE_SECRET"),
    psecsBaseUrl: cleanUrl(process.env.PSECS_BASE_URL ?? DEFAULT_PSECS_BASE_URL),
    auth0IssuerUrl: `https://${auth0Domain}/`,
    mcpBaseUrl: cleanUrl(process.env.MCP_BASE_URL ?? DEFAULT_MCP_BASE_URL),
  };
}

function requireEnv(name: string): string {
  const value = process.env[name];
  if (!value) {
    throw new Error(`${name} environment variable is required in OAuth proxy mode.`);
  }
  return value;
}

function validateAuth0Domain(domain: string): void {
  if (domain.includes("://") || domain.includes("/") || domain.includes("?") || domain.includes("@")) {
    throw new Error(
      `AUTH0_DOMAIN must be a plain hostname (e.g., myapp.us.auth0.com). Got: "${domain}"`
    );
  }
}

function cleanUrl(url: string): string {
  return url.replace(/\/+$/, "");
}

/**
 * Exchange an Auth0 authorization code for tokens (server-to-server).
 * Returns the ID token's `sub` claim (the user's Auth0 ID).
 */
export async function exchangeAuth0Code(
  code: string,
  config: OAuthProxyConfig
): Promise<{ ok: true; userId: string } | { ok: false; error: string }> {
  try {
    const response = await fetch(`${config.auth0IssuerUrl}oauth/token`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        grant_type: "authorization_code",
        client_id: config.auth0ClientId,
        client_secret: config.auth0ClientSecret,
        code,
        redirect_uri: `${config.mcpBaseUrl}/oauth/callback`,
      }),
    });

    if (!response.ok) {
      const body = await response.text();
      return { ok: false, error: `Auth0 token exchange failed: HTTP ${response.status} — ${body}` };
    }

    const body = (await response.json()) as { id_token?: string };
    if (!body.id_token) {
      return { ok: false, error: "Auth0 response missing id_token" };
    }

    // Decode the ID token to get the sub claim.
    // We trust it because it came directly from Auth0 over HTTPS using our client_secret.
    const [, payloadB64] = body.id_token.split(".");
    const payload = JSON.parse(Buffer.from(payloadB64, "base64url").toString());
    if (!payload.sub) {
      return { ok: false, error: "Auth0 id_token missing sub claim" };
    }

    return { ok: true, userId: payload.sub };
  } catch (err) {
    return {
      ok: false,
      error: err instanceof Error ? err.message : "Auth0 code exchange failed",
    };
  }
}

/**
 * Call the PSECS API internal endpoint to get or create an API key for a user.
 */
export async function provisionApiKey(
  userId: string,
  config: { psecsBaseUrl: string; serviceSecret: string }
): Promise<ProvisionResult> {
  try {
    const response = await fetch(`${config.psecsBaseUrl}/internal/mcp/api-key`, {
      method: "POST",
      headers: {
        "X-Service-Key": config.serviceSecret,
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ userId }),
    });

    if (!response.ok) {
      return { ok: false, error: `API key provisioning failed: HTTP ${response.status}` };
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
      error: err instanceof Error ? err.message : "API key provisioning network error",
    };
  }
}
