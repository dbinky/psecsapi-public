import type { PsecsConfig } from "./config.js";

export type ErrorType = "auth" | "game" | "rate_limit" | "infrastructure";

export interface ApiResponse<T> {
  ok: true;
  data: T;
  status: number;
}

export interface ApiError {
  ok: false;
  errorType: ErrorType;
  message: string;
  status: number;
  retryAfter?: number;
  currentLimit?: number;
}

export type ApiResult<T> = ApiResponse<T> | ApiError;

export interface RequestOptions {
  path?: Record<string, string>;
  query?: Record<string, string | number | boolean | undefined>;
}

export class PsecsClient {
  private readonly apiKey: string;
  private readonly baseUrl: string;

  constructor(config: PsecsConfig) {
    this.apiKey = config.apiKey;
    this.baseUrl = config.baseUrl;
  }

  async get<T>(path: string, options?: RequestOptions): Promise<ApiResult<T>> {
    return this.request<T>("GET", path, undefined, options);
  }

  async post<T>(
    path: string,
    body?: unknown,
    options?: RequestOptions
  ): Promise<ApiResult<T>> {
    return this.request<T>("POST", path, body, options);
  }

  async put<T>(
    path: string,
    body?: unknown,
    options?: RequestOptions
  ): Promise<ApiResult<T>> {
    return this.request<T>("PUT", path, body, options);
  }

  async delete<T>(
    path: string,
    options?: RequestOptions
  ): Promise<ApiResult<T>> {
    return this.request<T>("DELETE", path, undefined, options);
  }

  private async request<T>(
    method: string,
    path: string,
    body: unknown,
    options?: RequestOptions,
    retryCount = 0
  ): Promise<ApiResult<T>> {
    const url = this.buildUrl(path, options);

    const headers: Record<string, string> = {
      Authorization: `Bearer ${this.apiKey}`,
    };

    if (body !== undefined) {
      headers["Content-Type"] = "application/json";
    }

    let response: Response;
    try {
      response = await fetch(url, {
        method,
        headers,
        body: body !== undefined ? JSON.stringify(body) : undefined,
      });
    } catch (err) {
      return {
        ok: false,
        errorType: "infrastructure",
        message: err instanceof Error ? err.message : "Network error",
        status: 0,
      };
    }

    if (response.ok) {
      const data = (await response.json()) as T;
      return { ok: true, data, status: response.status };
    }

    // Auto-retry on 429 (rate limit) up to 3 times with backoff
    if (response.status === 429 && retryCount < 3) {
      const retryAfter = response.headers.get("Retry-After");
      const delayMs = retryAfter ? parseInt(retryAfter, 10) * 1000 : 1000;
      await new Promise((resolve) => setTimeout(resolve, delayMs));
      return this.request<T>(method, path, body, options, retryCount + 1);
    }

    const errorType = this.classifyError(response.status);
    const result: ApiError = {
      ok: false,
      errorType,
      message: "",
      status: response.status,
    };

    if (response.status === 429) {
      const retryAfter = response.headers.get("Retry-After");
      if (retryAfter) {
        result.retryAfter = parseInt(retryAfter, 10);
      }
      // Parse JSON body once to get both message and currentLimit
      const parsed = await this.parseRateLimitBody(response);
      result.message = parsed.message;
      result.currentLimit = parsed.currentLimit;
    } else {
      result.message = await this.parseErrorBody(response);
    }

    return result;
  }

  private buildUrl(path: string, options?: RequestOptions): string {
    let resolvedPath = path;

    if (options?.path) {
      for (const [key, value] of Object.entries(options.path)) {
        resolvedPath = resolvedPath.replace(`{${key}}`, value);
      }
    }

    const url = new URL(resolvedPath, this.baseUrl);

    if (options?.query) {
      for (const [key, value] of Object.entries(options.query)) {
        if (value !== undefined) {
          url.searchParams.set(key, String(value));
        }
      }
    }

    return url.toString();
  }

  private classifyError(status: number): ErrorType {
    if (status === 401) return "auth";
    if (status === 429) return "rate_limit";
    if (status >= 400 && status < 500) return "game"; // 403 = AccessDenied (game-logic), not an API key failure
    return "infrastructure";
  }

  private async parseRateLimitBody(
    response: Response
  ): Promise<{ message: string; currentLimit?: number }> {
    try {
      const body = await response.json();
      const message = sanitizeErrorMessage(
        typeof body.message === "string" ? body.message : `HTTP 429 error`
      );
      const currentLimit =
        typeof body.currentLimit === "number" ? body.currentLimit : undefined;
      return { message, currentLimit };
    } catch {
      return { message: "HTTP 429 error" };
    }
  }

  private async parseErrorBody(response: Response): Promise<string> {
    try {
      const body = await response.json();
      // Orleans domain exceptions serialize with PascalCase DomainExceptionMessage.
      // Generic 500s from GlobalExceptionHandlerMiddleware use { error: "..." }.
      const raw: unknown =
        body.message ??
        body.errorMessage ??
        body.DomainExceptionMessage ??
        body.error ??
        `HTTP ${response.status} error`;
      return sanitizeErrorMessage(typeof raw === "string" ? raw : String(raw));
    } catch {
      try {
        const text = await response.text();
        if (text) return sanitizeErrorMessage(text);
      } catch {
        // response body already consumed or unreadable
      }
      return `HTTP ${response.status} error`;
    }
  }
}

const MAX_ERROR_MESSAGE_LENGTH = 500;

/**
 * Sanitize error messages before they reach the AI agent context.
 * Truncates to a safe length and strips control characters to reduce
 * the risk of prompt injection via crafted error responses.
 */
function sanitizeErrorMessage(message: string): string {
  // Strip control characters (except newline and tab which are benign)
  const cleaned = message.replace(/[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]/g, "");
  if (cleaned.length <= MAX_ERROR_MESSAGE_LENGTH) return cleaned;
  return cleaned.slice(0, MAX_ERROR_MESSAGE_LENGTH) + "... (truncated)";
}
