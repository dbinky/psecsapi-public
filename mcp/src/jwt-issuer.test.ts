import { describe, it, expect, beforeAll } from "vitest";
import { JwtIssuer } from "./jwt-issuer.js";
import { jwtVerify, createLocalJWKSet } from "jose";

describe("JwtIssuer", () => {
  let issuer: JwtIssuer;

  beforeAll(async () => {
    issuer = await JwtIssuer.createInMemory();
  });

  it("signs a JWT that can be verified with the JWKS", async () => {
    const token = await issuer.sign({ sub: "auth0|user123", scope: "psecs:play" });
    const jwks = issuer.getJwks();
    const keySet = createLocalJWKSet(jwks);
    const { payload } = await jwtVerify(token, keySet, {
      issuer: "https://mcp.psecsapi.com",
      audience: "https://mcp.psecsapi.com",
    });
    expect(payload.sub).toBe("auth0|user123");
    expect(payload.scope).toBe("psecs:play");
  });

  it("includes kid in JWT header matching JWKS key", async () => {
    const token = await issuer.sign({ sub: "auth0|user123", scope: "psecs:play" });
    const [headerB64] = token.split(".");
    const header = JSON.parse(Buffer.from(headerB64, "base64url").toString());
    const jwks = issuer.getJwks();
    expect(header.kid).toBe(jwks.keys[0].kid);
  });

  it("sets iss, aud, exp, iat claims", async () => {
    const token = await issuer.sign({ sub: "auth0|user123", scope: "psecs:play" });
    const jwks = issuer.getJwks();
    const keySet = createLocalJWKSet(jwks);
    const { payload } = await jwtVerify(token, keySet);
    expect(payload.iss).toBe("https://mcp.psecsapi.com");
    expect(payload.aud).toBe("https://mcp.psecsapi.com");
    expect(payload.exp).toBeDefined();
    expect(payload.iat).toBeDefined();
    expect(payload.exp! - payload.iat!).toBe(3600);
  });

  it("rejects a token signed with a different key", async () => {
    const otherIssuer = await JwtIssuer.createInMemory();
    const token = await otherIssuer.sign({ sub: "auth0|user123", scope: "psecs:play" });
    const jwks = issuer.getJwks();
    const keySet = createLocalJWKSet(jwks);
    await expect(jwtVerify(token, keySet)).rejects.toThrow();
  });

  it("getJwks returns valid JWKS with RS256 public key", () => {
    const jwks = issuer.getJwks();
    expect(jwks.keys).toHaveLength(1);
    expect(jwks.keys[0].kty).toBe("RSA");
    expect(jwks.keys[0].alg).toBe("RS256");
    expect(jwks.keys[0].use).toBe("sig");
    expect(jwks.keys[0].kid).toBeDefined();
    expect(jwks.keys[0]).not.toHaveProperty("d");
    expect(jwks.keys[0]).not.toHaveProperty("p");
    expect(jwks.keys[0]).not.toHaveProperty("q");
  });
});
