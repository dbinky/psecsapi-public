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
  if (error.errorType === "rate_limit" && error.retryAfter) {
    const unit = error.retryAfter === 1 ? "second" : "seconds";
    parts.push(
      `Rate limited. Retry after ${error.retryAfter} ${unit}. ` +
      `To permanently increase your rate limit, stake more tokens using psecs_raw_create_user_stake_api_tokens.`
    );
  }
  return {
    isError: isSystemError,
    content: [{ type: "text" as const, text: parts.join("\n") }],
  };
}
