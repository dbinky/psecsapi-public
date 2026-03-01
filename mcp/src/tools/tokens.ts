import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { PsecsClient } from "../client.js";
import { formatToolResult, formatToolError } from "../tool-utils.js";

interface BalanceResponse {
  tokens: number;
  stakedTokens: number;
  availableTokens: number;
}

interface StakeInfoResponse {
  stakedTokens: number;
  availableTokens: number;
  currentRateLimit: number;
  cooldownEndsAt: string | null;
}

interface StakeResponse {
  newStakedAmount: number;
  newAvailableTokens: number;
  newRateLimit: number;
  accessToken?: string;
}

interface UnstakeResponse {
  newStakedAmount: number;
  newAvailableTokens: number;
  newRateLimit: number;
  cooldownEndsAt: string;
  accessToken?: string;
}

const RATE_LIMIT_TIERS = [
  { staked: 0, limit: 2 },
  { staked: 0.1, limit: 5 },
  { staked: 0.25, limit: 10 },
  { staked: 0.5, limit: 20 },
  { staked: 1, limit: 33 },
  { staked: 2, limit: 50 },
  { staked: 5, limit: 75 },
  { staked: 10, limit: 100 },
];

const TOKEN_PURCHASE_URL = "https://www.psecsapi.com/account/tokens";

export function registerTokenTools(
  server: McpServer,
  client: PsecsClient
): void {
  server.registerTool(
    "psecs_token_status",
    {
      description:
        "Check your token balance, staking status, and current API rate limit. " +
        "Returns suggestions for improving your rate limit through token staking.",
    },
    async () => {
      const suggestions: string[] = [];

      const [balanceResult, stakeResult] = await Promise.all([
        client.get<BalanceResponse>("/api/tokens/balance"),
        client.get<StakeInfoResponse>("/api/User/api-stake-info"),
      ]);

      if (!balanceResult.ok) return formatToolError(balanceResult);
      if (!stakeResult.ok) return formatToolError(stakeResult);

      const balance = balanceResult.data;
      const stake = stakeResult.data;

      if (balance.tokens === 0 && balance.stakedTokens === 0) {
        suggestions.push(
          `No tokens yet. Purchase tokens at ${TOKEN_PURCHASE_URL} ($10 each).`
        );
      } else if (balance.availableTokens > 0 && stake.currentRateLimit < 100) {
        suggestions.push(
          `You have ${balance.availableTokens} unstaked tokens. Use psecs_stake_tokens to boost your rate limit.`
        );
      }

      // Find next tier
      const nextTier = RATE_LIMIT_TIERS.find(
        (t) => t.staked > stake.stakedTokens
      );
      if (nextTier) {
        const needed = nextTier.staked - stake.stakedTokens;
        suggestions.push(
          `Next tier: stake ${needed} more token(s) to reach ${nextTier.limit} req/s.`
        );
      }

      if (stake.cooldownEndsAt) {
        suggestions.push(
          `Unstake cooldown active until ${stake.cooldownEndsAt}. You cannot unstake until then.`
        );
      }

      return formatToolResult({
        totalTokens: balance.tokens,
        stakedTokens: balance.stakedTokens,
        availableTokens: balance.availableTokens,
        currentRateLimit: stake.currentRateLimit,
        cooldownEndsAt: stake.cooldownEndsAt,
        purchaseUrl: TOKEN_PURCHASE_URL,
        rateLimitTiers: RATE_LIMIT_TIERS,
        suggestions,
      });
    }
  );

  server.registerTool(
    "psecs_stake_tokens",
    {
      description:
        "Stake tokens to increase your API rate limit. Staked tokens are NOT consumed — " +
        "they remain yours and can be unstaked later (with a 1-hour cooldown). " +
        "Even 1 staked token boosts your rate limit from 2 to 33 req/s.",
      inputSchema: {
        amount: z
          .number()
          .min(0.1)
          .max(10)
          .describe("Amount of tokens to stake (min 0.1, max total stake 10)"),
      },
    },
    async (args) => {
      const result = await client.post<StakeResponse>(
        "/api/User/stake-api-tokens",
        { amount: args.amount }
      );
      if (!result.ok) return formatToolError(result);

      const suggestions: string[] = [];
      const data = result.data;

      suggestions.push(
        `Rate limit updated to ${data.newRateLimit} req/s.`
      );

      const nextTier = RATE_LIMIT_TIERS.find(
        (t) => t.staked > data.newStakedAmount
      );
      if (nextTier) {
        const needed = nextTier.staked - data.newStakedAmount;
        suggestions.push(
          `Next tier: stake ${needed} more to reach ${nextTier.limit} req/s.`
        );
      }

      return formatToolResult({
        stakedTokens: data.newStakedAmount,
        availableTokens: data.newAvailableTokens,
        newRateLimit: data.newRateLimit,
        suggestions,
      });
    }
  );

  server.registerTool(
    "psecs_unstake_tokens",
    {
      description:
        "Unstake tokens from your API rate limit stake. Returns tokens to your available balance. " +
        "Triggers a 1-hour cooldown before you can unstake again.",
      inputSchema: {
        amount: z
          .number()
          .min(0.1)
          .max(10)
          .describe("Amount of tokens to unstake"),
      },
    },
    async (args) => {
      const result = await client.post<UnstakeResponse>(
        "/api/User/unstake-api-tokens",
        { amount: args.amount }
      );
      if (!result.ok) return formatToolError(result);

      const data = result.data;

      return formatToolResult({
        stakedTokens: data.newStakedAmount,
        availableTokens: data.newAvailableTokens,
        newRateLimit: data.newRateLimit,
        cooldownEndsAt: data.cooldownEndsAt,
        warning:
          "Unstake cooldown is now active for 1 hour. You cannot unstake again until the cooldown expires.",
      });
    }
  );
}
