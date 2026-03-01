import type { ApiError } from "./client.js";

export function formatToolResult(data: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(data, null, 2) }],
  };
}

/**
 * MCP errors that represent system/infrastructure failures (isError: true)
 * vs game-logic rejections that are normal responses (isError: false).
 */
const SYSTEM_ERROR_TYPES: ReadonlySet<string> = new Set([
  "auth",
  "rate_limit",
  "infrastructure",
]);

export function formatToolError(error: ApiError) {
  const isSystemError = SYSTEM_ERROR_TYPES.has(error.errorType);
  const parts = [`Error: ${error.message}`];
  if (error.errorType === "auth") {
    parts.push(
      "Your API key may be invalid or revoked. Get a new one from the PSECS web UI."
    );
  }
  if (error.errorType === "rate_limit") {
    const limitInfo = error.currentLimit
      ? ` (current limit: ${error.currentLimit} req/s)`
      : "";
    if (error.retryAfter) {
      const unit = error.retryAfter === 1 ? "second" : "seconds";
      parts.push(
        `Rate limited${limitInfo}. Retry after ${error.retryAfter} ${unit}.`
      );
    } else {
      parts.push(`Rate limited${limitInfo}.`);
    }
    if (error.currentLimit === 2) {
      parts.push(
        "You're on the base rate limit. Even 1 staked token boosts you from 2 to 33 req/s."
      );
    }
    parts.push(
      "Check psecs_token_status for your balance and rate limit info.\n" +
      "Purchase tokens at https://www.psecsapi.com/account/tokens"
    );
    parts.push(
      "If you have unstaked tokens, use psecs_stake_tokens to boost your rate limit immediately."
    );
  }
  return {
    isError: isSystemError,
    content: [{ type: "text" as const, text: parts.join("\n") }],
  };
}
