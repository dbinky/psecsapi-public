import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { formatToolResult } from "../tool-utils.js";

const PLATFORM_MAP: Record<string, { rid: string; extension: string }> = {
  "macos-arm64": { rid: "osx-arm64", extension: "" },
  "macos-x64": { rid: "osx-x64", extension: "" },
  "linux-x64": { rid: "linux-x64", extension: "" },
  "windows-x64": { rid: "win-x64", extension: ".exe" },
  "windows-arm64": { rid: "win-arm64", extension: ".exe" },
};

const BASE_URL =
  "https://github.com/dbinky/psecsapi-public/releases/latest/download";

export function registerCliTools(server: McpServer): void {
  server.registerTool(
    "psecs_download_cli",
    {
      description:
        "Get the direct download URL and install instructions for the PSECS CLI (papi). " +
        "The CLI enables automation, scripting, and direct API access from your terminal.",
      inputSchema: {
        platform: z
          .enum([
            "macos-arm64",
            "macos-x64",
            "linux-x64",
            "windows-x64",
            "windows-arm64",
          ])
          .optional()
          .describe(
            "Target platform. If omitted, returns URLs for all platforms."
          ),
      },
    },
    async (args) => {
      const platforms = args.platform
        ? [args.platform]
        : Object.keys(PLATFORM_MAP);

      const downloads = platforms.map((platform) => {
        const { rid, extension } = PLATFORM_MAP[platform];
        const filename = `papi-${rid}${extension}`;
        const url = `${BASE_URL}/${filename}`;
        const isWindows = platform.startsWith("windows");

        const installSteps = isWindows
          ? [
              `Download: ${url}`,
              `Move papi-${rid}.exe to a folder in your PATH`,
              `Rename to papi.exe if desired`,
            ]
          : [
              `curl -L -o papi ${url}`,
              `chmod +x papi`,
              `sudo mv papi /usr/local/bin/`,
            ];

        return { platform, url, filename, installSteps };
      });

      const setupSteps = [
        "papi auth login    # Authenticate with your PSECS account",
        "papi status         # Check connection and account status",
        "papi --help         # See all available commands",
      ];

      return formatToolResult({
        downloads,
        setupSteps,
        sourceRepo: "https://github.com/dbinky/psecsapi-public",
        note: "Use curl -L (follow redirects) to download. Do NOT download HTML pages.",
      });
    }
  );
}
