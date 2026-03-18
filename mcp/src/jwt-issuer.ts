import { SignJWT, exportJWK, importJWK, generateKeyPair, type JWK } from "jose";
import crypto from "node:crypto";

const ISSUER = "https://mcp.psecsapi.com";
const AUDIENCE = "https://mcp.psecsapi.com";
const TOKEN_LIFETIME_SECONDS = 3600;

export class JwtIssuer {
  private constructor(
    private readonly privateKey: CryptoKey,
    private readonly publicJwk: JWK,
    private readonly kid: string
  ) {}

  /** Create from env var MCP_SIGNING_KEY (JSON-encoded JWK). Falls back to in-memory. */
  static async create(): Promise<JwtIssuer> {
    const keyJson = process.env.MCP_SIGNING_KEY;
    if (keyJson) {
      return JwtIssuer.createFromJwk(keyJson);
    }
    console.error("[psecs-mcp] No MCP_SIGNING_KEY set, generating ephemeral key (tokens won't survive restart)");
    return JwtIssuer.createInMemory();
  }

  /** Load a pre-generated private key from a JSON-encoded JWK string. */
  static async createFromJwk(jwkJson: string): Promise<JwtIssuer> {
    const fullJwk = JSON.parse(jwkJson) as JWK;
    const privateKey = await importJWK(fullJwk, "RS256") as CryptoKey;

    // Derive public JWK (strip private fields)
    const publicJwk: JWK = {
      kty: fullJwk.kty,
      n: fullJwk.n,
      e: fullJwk.e,
      kid: fullJwk.kid,
      alg: "RS256",
      use: "sig",
    };

    return new JwtIssuer(privateKey, publicJwk, fullJwk.kid!);
  }

  static async createInMemory(): Promise<JwtIssuer> {
    const { privateKey, publicKey } = await generateKeyPair("RS256");
    const jwk = await exportJWK(publicKey);
    const kid = crypto.randomUUID();
    jwk.kid = kid;
    jwk.alg = "RS256";
    jwk.use = "sig";
    return new JwtIssuer(privateKey, jwk, kid);
  }

  /** Export the full private JWK (for persisting to env var or Key Vault). */
  async exportPrivateJwk(): Promise<JWK> {
    const jwk = await exportJWK(this.privateKey);
    jwk.kid = this.kid;
    jwk.alg = "RS256";
    jwk.use = "sig";
    return jwk;
  }

  static async createFromKeyVault(vaultName: string): Promise<JwtIssuer> {
    console.error("[psecs-mcp] Key Vault integration not yet implemented, using in-memory key");
    return JwtIssuer.createInMemory();
  }

  async sign(claims: { sub: string; scope: string }): Promise<string> {
    return new SignJWT({ ...claims })
      .setProtectedHeader({ alg: "RS256", kid: this.kid })
      .setIssuer(ISSUER)
      .setAudience(AUDIENCE)
      .setIssuedAt()
      .setExpirationTime(`${TOKEN_LIFETIME_SECONDS}s`)
      .sign(this.privateKey);
  }

  getJwks(): { keys: JWK[] } {
    return { keys: [this.publicJwk] };
  }
}
