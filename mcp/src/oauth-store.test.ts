import { describe, it, expect, beforeEach, afterEach, vi } from "vitest";
import { OAuthStore } from "./oauth-store.js";

describe("OAuthStore", () => {
  let store: OAuthStore;
  beforeEach(() => { store = OAuthStore.createInMemory(); });
  afterEach(() => { store.destroy(); });

  describe("DCR clients", () => {
    it("registers and retrieves a client", () => {
      const clientId = store.registerClient({
        clientName: "Claude",
        redirectUris: ["https://claude.ai/api/mcp/auth_callback"],
      });
      const client = store.getClient(clientId);
      expect(client).toBeDefined();
      expect(client!.clientName).toBe("Claude");
      expect(client!.redirectUris).toEqual(["https://claude.ai/api/mcp/auth_callback"]);
    });

    it("returns undefined for unknown client", () => {
      expect(store.getClient("nonexistent")).toBeUndefined();
    });
  });

  describe("auth sessions", () => {
    it("stores and retrieves an auth session", () => {
      const sessionId = store.createAuthSession({
        clientId: "client-1", redirectUri: "https://claude.ai/api/mcp/auth_callback",
        codeChallenge: "abc123", clientState: "state-xyz", scope: "psecs:play", csrfToken: "csrf-token",
      });
      const session = store.getAuthSession(sessionId);
      expect(session).toBeDefined();
      expect(session!.clientId).toBe("client-1");
      expect(session!.codeChallenge).toBe("abc123");
    });

    it("consumes session (one-time use)", () => {
      const sessionId = store.createAuthSession({
        clientId: "c", redirectUri: "https://x.com/cb", codeChallenge: "abc",
        clientState: "s", scope: "psecs:play", csrfToken: "csrf",
      });
      expect(store.consumeAuthSession(sessionId)).toBeDefined();
      expect(store.getAuthSession(sessionId)).toBeUndefined();
    });
  });

  describe("auth codes", () => {
    it("stores and retrieves an auth code", () => {
      const code = store.createAuthCode({
        clientId: "c", userId: "auth0|user123", apiKey: "psecs_sk_test",
        codeChallenge: "ch", redirectUri: "https://claude.ai/cb", clientState: "s", scope: "psecs:play",
      });
      const data = store.consumeAuthCode(code);
      expect(data).toBeDefined();
      expect(data!.userId).toBe("auth0|user123");
      expect(data!.apiKey).toBe("psecs_sk_test");
    });

    it("consumes code (one-time use)", () => {
      const code = store.createAuthCode({
        clientId: "c", userId: "u", apiKey: "k", codeChallenge: "ch",
        redirectUri: "https://x.com/cb", clientState: "s", scope: "psecs:play",
      });
      expect(store.consumeAuthCode(code)).toBeDefined();
      expect(store.consumeAuthCode(code)).toBeUndefined();
    });

    it("rejects expired codes", () => {
      vi.useFakeTimers();
      const code = store.createAuthCode({
        clientId: "c", userId: "u", apiKey: "k", codeChallenge: "ch",
        redirectUri: "https://x.com/cb", clientState: "s", scope: "psecs:play",
      });
      vi.advanceTimersByTime(61_000);
      expect(store.consumeAuthCode(code)).toBeUndefined();
      vi.useRealTimers();
    });
  });

  describe("API key cache", () => {
    it("stores and retrieves an API key", async () => {
      await store.setApiKey("auth0|user123", "psecs_sk_test");
      expect(await store.getApiKey("auth0|user123")).toBe("psecs_sk_test");
    });

    it("returns undefined for unknown user", async () => {
      expect(await store.getApiKey("auth0|nonexistent")).toBeUndefined();
    });
  });

  describe("refresh tokens", () => {
    it("stores and retrieves a refresh token", async () => {
      const token = await store.createRefreshToken({
        userId: "auth0|user123", clientId: "c", apiKey: "psecs_sk_test",
      });
      const data = await store.consumeRefreshToken(token);
      expect(data).toBeDefined();
      expect(data!.userId).toBe("auth0|user123");
    });

    it("rotates: old token invalid after consume", async () => {
      const token = await store.createRefreshToken({
        userId: "auth0|user123", clientId: "c", apiKey: "psecs_sk_test",
      });
      await store.consumeRefreshToken(token);
      expect(await store.consumeRefreshToken(token)).toBeUndefined();
    });

    it("replay detection: revokes all user tokens on replay", async () => {
      const token1 = await store.createRefreshToken({
        userId: "auth0|user123", clientId: "c", apiKey: "psecs_sk_test",
      });
      await store.consumeRefreshToken(token1);
      const token2 = await store.createRefreshToken({
        userId: "auth0|user123", clientId: "c", apiKey: "psecs_sk_test",
      });
      // Replay token1
      expect(await store.consumeRefreshToken(token1)).toBeUndefined();
      // token2 should also be revoked
      expect(await store.consumeRefreshToken(token2)).toBeUndefined();
    });
  });
});
