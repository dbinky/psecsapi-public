import { describe, it, expect } from "vitest";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";
import { PersistentOAuthStore } from "./persistent-oauth-store.js";

function tmpFile(): string {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), "mcp-oauth-store-"));
  return path.join(dir, "oauth-store.json");
}

describe("PersistentOAuthStore", () => {
  it("persists DCR clients across a reload", () => {
    const file = tmpFile();
    const s1 = PersistentOAuthStore.createPersistent(file);
    const id = s1.registerClient({ clientName: "claude", redirectUris: ["https://claude.ai/cb"] });
    s1.destroy();

    const s2 = PersistentOAuthStore.createPersistent(file);
    expect(s2.getClient(id)).toEqual({ clientName: "claude", redirectUris: ["https://claude.ai/cb"] });
    s2.destroy();
  });

  it("persists refresh tokens across a reload", async () => {
    const file = tmpFile();
    const s1 = PersistentOAuthStore.createPersistent(file);
    const token = await s1.createRefreshToken({ userId: "u1", clientId: "c1", apiKey: "k1" });
    s1.destroy();

    const s2 = PersistentOAuthStore.createPersistent(file);
    expect(await s2.consumeRefreshToken(token)).toEqual({ userId: "u1", clientId: "c1", apiKey: "k1" });
    s2.destroy();
  });

  it("still detects refresh-token replay after a reload", async () => {
    const file = tmpFile();
    const s1 = PersistentOAuthStore.createPersistent(file);
    const token = await s1.createRefreshToken({ userId: "u1", clientId: "c1", apiKey: "k1" });
    await s1.consumeRefreshToken(token); // legitimate single use
    s1.destroy();

    const s2 = PersistentOAuthStore.createPersistent(file);
    expect(await s2.consumeRefreshToken(token)).toBeUndefined(); // replay rejected
    s2.destroy();
  });

  it("persists the API key cache across a reload", async () => {
    const file = tmpFile();
    const s1 = PersistentOAuthStore.createPersistent(file);
    await s1.setApiKey("u1", "secret-key");
    s1.destroy();

    const s2 = PersistentOAuthStore.createPersistent(file);
    expect(await s2.getApiKey("u1")).toBe("secret-key");
    s2.destroy();
  });

  it("starts empty when the file is missing", () => {
    const s = PersistentOAuthStore.createPersistent(tmpFile());
    expect(s.getClient("nope")).toBeUndefined();
    s.destroy();
  });

  it("starts empty (and does not throw) when the file is corrupt", () => {
    const file = tmpFile();
    fs.writeFileSync(file, "}{ not valid json");
    const s = PersistentOAuthStore.createPersistent(file);
    expect(s.getClient("nope")).toBeUndefined();
    // a subsequent registration overwrites the corrupt file cleanly
    const id = s.registerClient({ clientName: "x", redirectUris: ["https://x/cb"] });
    expect(s.getClient(id)).toBeDefined();
    s.destroy();
  });
});
