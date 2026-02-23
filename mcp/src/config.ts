export interface PsecsConfig {
  apiKey: string;
  baseUrl: string;
}

const DEFAULT_BASE_URL = "https://api.psecs.io";

export function loadConfig(): PsecsConfig {
  const apiKey = process.env.PSECS_API_KEY;
  if (!apiKey) {
    throw new Error(
      "PSECS_API_KEY environment variable is required. " +
        "Generate an API key from the PSECS web UI at https://psecs.io/api-keys"
    );
  }

  const baseUrl = (process.env.PSECS_BASE_URL ?? DEFAULT_BASE_URL).replace(
    /\/$/,
    ""
  );

  if (!/^https?:\/\//i.test(baseUrl)) {
    throw new Error(
      `Invalid PSECS_BASE_URL: "${baseUrl}". Must start with http:// or https://.`
    );
  }

  return { apiKey, baseUrl };
}
