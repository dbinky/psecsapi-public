import crypto from "node:crypto";

export interface DcrClient {
  clientName: string;
  redirectUris: string[];
}

export interface AuthSession {
  clientId: string;
  redirectUri: string;
  codeChallenge: string;
  clientState: string;
  scope: string;
  csrfToken: string;
}

export interface AuthCodeData {
  clientId: string;
  userId: string;
  apiKey: string;
  codeChallenge: string;
  redirectUri: string;
  clientState: string;
  scope: string;
  expiresAt: number;
}

export interface RefreshTokenData {
  userId: string;
  clientId: string;
  apiKey: string;
}

const AUTH_CODE_TTL_MS = 60_000;
const AUTH_CODE_MAX_ENTRIES = 10_000;
const DCR_CLIENT_MAX_ENTRIES = 10_000;
const SWEEP_INTERVAL_MS = 60_000;

export class OAuthStore {
  private clients = new Map<string, DcrClient>();
  private authSessions = new Map<string, AuthSession>();
  private authCodes = new Map<string, AuthCodeData>();
  private apiKeys = new Map<string, string>();
  private refreshTokens = new Map<string, RefreshTokenData>();
  private consumedTokens = new Map<string, string>();
  private sweepTimer: ReturnType<typeof setInterval> | null = null;

  protected constructor() {}

  static createInMemory(): OAuthStore {
    const store = new OAuthStore();
    store.startSweepTimer();
    return store;
  }

  protected startSweepTimer(): void {
    this.sweepTimer = setInterval(() => this.sweep(), SWEEP_INTERVAL_MS);
  }

  // --- DCR Clients ---
  registerClient(client: DcrClient): string {
    if (this.clients.size >= DCR_CLIENT_MAX_ENTRIES) {
      throw new Error("DCR client store is full");
    }
    const clientId = crypto.randomUUID();
    this.clients.set(clientId, client);
    return clientId;
  }

  getClient(clientId: string): DcrClient | undefined {
    return this.clients.get(clientId);
  }

  // --- Auth Sessions ---
  createAuthSession(session: AuthSession): string {
    const sessionId = crypto.randomUUID();
    this.authSessions.set(sessionId, session);
    return sessionId;
  }

  getAuthSession(sessionId: string): AuthSession | undefined {
    return this.authSessions.get(sessionId);
  }

  consumeAuthSession(sessionId: string): AuthSession | undefined {
    const session = this.authSessions.get(sessionId);
    this.authSessions.delete(sessionId);
    return session;
  }

  // --- Auth Codes ---
  createAuthCode(data: Omit<AuthCodeData, "expiresAt">): string {
    if (this.authCodes.size >= AUTH_CODE_MAX_ENTRIES) {
      throw new Error("Authorization code store is full");
    }
    const code = crypto.randomBytes(32).toString("base64url");
    this.authCodes.set(code, { ...data, expiresAt: Date.now() + AUTH_CODE_TTL_MS });
    return code;
  }

  consumeAuthCode(code: string): AuthCodeData | undefined {
    const data = this.authCodes.get(code);
    this.authCodes.delete(code);
    if (!data || data.expiresAt < Date.now()) return undefined;
    return data;
  }

  private sweep(): void {
    const now = Date.now();
    for (const [code, data] of this.authCodes) {
      if (data.expiresAt < now) this.authCodes.delete(code);
    }
    if (this.consumedTokens.size > 100_000) this.consumedTokens.clear();
  }

  // --- API Key Cache ---
  async getApiKey(userId: string): Promise<string | undefined> {
    return this.apiKeys.get(userId);
  }

  async setApiKey(userId: string, apiKey: string): Promise<void> {
    this.apiKeys.set(userId, apiKey);
  }

  // --- Refresh Tokens ---
  async createRefreshToken(data: RefreshTokenData): Promise<string> {
    const token = crypto.randomBytes(32).toString("base64url");
    const hash = this.hashToken(token);
    this.refreshTokens.set(hash, data);
    return token;
  }

  async consumeRefreshToken(token: string): Promise<RefreshTokenData | undefined> {
    const hash = this.hashToken(token);
    const revokedUserId = this.consumedTokens.get(hash);
    if (revokedUserId) {
      this.revokeAllUserTokens(revokedUserId);
      return undefined;
    }
    const data = this.refreshTokens.get(hash);
    if (!data) return undefined;
    this.refreshTokens.delete(hash);
    this.consumedTokens.set(hash, data.userId);
    return data;
  }

  private revokeAllUserTokens(userId: string): void {
    for (const [hash, data] of this.refreshTokens) {
      if (data.userId === userId) this.refreshTokens.delete(hash);
    }
  }

  protected hashToken(token: string): string {
    return crypto.createHash("sha256").update(token).digest("hex");
  }

  destroy(): void {
    if (this.sweepTimer) { clearInterval(this.sweepTimer); this.sweepTimer = null; }
  }
}
