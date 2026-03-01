import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { PsecsClient } from "../client.js";
import { formatToolResult, formatToolError } from "../tool-utils.js";

interface MintRateResponse {
  currentRate: number;
  recentBurnVolume: number;
  baseRate: number;
  floorRate: number;
  windowHours: number;
}

interface MintBurnResponse {
  creditsReceived: number;
  rateApplied: number;
  tokensBurned: number;
  newTokenBalance: number;
  newCorpCredits: number;
}

export function registerMintTools(server: McpServer, client: PsecsClient): void {
  server.registerTool(
    "psecs_mint_rate",
    {
      description:
        "Check the current token-to-credit exchange rate at the Credit Mint. " +
        "The rate is dynamic — it drops when many players burn tokens and recovers over 24 hours. " +
        "Always check the rate before burning tokens.",
    },
    async () => {
      const result = await client.get<MintRateResponse>("/api/mint/rate");
      if (!result.ok) return formatToolError(result);
      const data = result.data;

      const ratePercent = Math.round((data.currentRate / data.baseRate) * 100);

      let suggestion = "";
      if (ratePercent > 80) {
        suggestion = "Rate is high — good time to burn.";
      } else if (ratePercent < 40) {
        suggestion = "Rate is depressed — consider waiting for it to recover.";
      } else {
        suggestion = "Rate is moderate.";
      }

      return formatToolResult({
        currentRate: data.currentRate,
        ratePercent,
        baseRate: data.baseRate,
        floorRate: data.floorRate,
        recentBurnVolume: data.recentBurnVolume,
        windowHours: data.windowHours,
        suggestion,
      });
    }
  );

  server.registerTool(
    "psecs_mint_burn",
    {
      description:
        "Permanently burn tokens to receive corp credits at the current exchange rate. " +
        "This is IRREVERSIBLE — burned tokens are destroyed forever. " +
        "Credits are deposited to your corp balance. You must have a corp. " +
        "Check psecs_mint_rate first to see the current rate.",
      inputSchema: {
        amount: z
          .number()
          .min(0.1)
          .max(100)
          .describe(
            "Number of tokens to burn (min 0.1, max 100, increments of 0.1)"
          ),
      },
    },
    async ({ amount }) => {
      const result = await client.post<MintBurnResponse>("/api/mint/burn", {
        amount,
      });
      if (!result.ok) return formatToolError(result);
      const data = result.data;

      return formatToolResult({
        tokensBurned: data.tokensBurned,
        rateApplied: data.rateApplied,
        creditsReceived: data.creditsReceived,
        newTokenBalance: data.newTokenBalance,
        newCorpCredits: data.newCorpCredits,
      });
    }
  );
}
