import fs from "node:fs";
import path from "node:path";
import { OAuthStore } from "./oauth-store.js";

/**
 * OAuthStore backed by a JSON file so durable state — DCR clients, refresh
 * tokens, replay-protection, and the API-key cache — survives container
 * restarts (e.g. every `deploy-local` --force-recreate). Without this, every
 * restart wipes registered clients and forces every connected client to
 * re-add the connector.
 *
 * Writes are synchronous and atomic (temp file + rename). The mutations that
 * trigger a write (client registration, refresh-token issue/consume) are
 * infrequent, so synchronous I/O is fine and keeps the in-memory map the
 * single source of truth.
 */
export class PersistentOAuthStore extends OAuthStore {
  private readonly filePath: string;
  private loading = false;

  private constructor(filePath: string) {
    super();
    this.filePath = filePath;
  }

  static createPersistent(filePath: string): PersistentOAuthStore {
    const store = new PersistentOAuthStore(filePath);
    store.load();
    store.startSweepTimer();
    return store;
  }

  private load(): void {
    this.loading = true;
    try {
      if (fs.existsSync(this.filePath)) {
        this.importDurable(JSON.parse(fs.readFileSync(this.filePath, "utf8")));
      }
    } catch (err) {
      console.error(
        `[psecs-mcp] OAuth store load failed (${this.filePath}), starting empty:`,
        err instanceof Error ? err.message : err
      );
    } finally {
      this.loading = false;
    }
  }

  protected override onDurableChange(): void {
    if (this.loading) return;
    try {
      fs.mkdirSync(path.dirname(this.filePath), { recursive: true });
      const tmp = `${this.filePath}.tmp`;
      fs.writeFileSync(tmp, JSON.stringify(this.exportDurable()), { mode: 0o600 });
      fs.renameSync(tmp, this.filePath);
    } catch (err) {
      console.error(
        `[psecs-mcp] OAuth store persist failed (${this.filePath}):`,
        err instanceof Error ? err.message : err
      );
    }
  }
}
