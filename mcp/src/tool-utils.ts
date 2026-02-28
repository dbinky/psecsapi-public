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
    const limitInfo = error.currentLimit
      ? ` (current limit: ${error.currentLimit} req/s)`
      : "";
    const unit = error.retryAfter === 1 ? "second" : "seconds";
    const webUrl = (process.env.PSECS_WEB_URL ?? "https://psecs.io").replace(
      /\/$/,
      ""
    );
    parts.push(
      `Rate limited${limitInfo}. Retry after ${error.retryAfter} ${unit}.\n` +
      `You can permanently increase your rate limit by purchasing and staking tokens.\n` +
      `See the guide: ${webUrl}/wiki/api-rate-limits`
    );
  }
  return {
    isError: isSystemError,
    content: [{ type: "text" as const, text: parts.join("\n") }],
  };
}
