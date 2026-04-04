import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { PsecsClient } from "../client.js";
import { formatToolResult, formatToolError } from "../tool-utils.js";

// Deployment note: the balance endpoint includes investedTokens, which requires
// the API to be deployed with the investment feature before this MCP server can
// rely on that field. If API deploys lag, investedTokens will be present as 0
// (the API always includes it from GetInvestmentInfo).
interface BalanceResponse {
  tokens: number;
  stakedTokens: number;
  investedTokens: number;
  availableTokens: number;
}

interface StakeInfoResponse {
  stakedTokens: number;
  availableTokens: number;
  rateLimit: number;
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

interface InvestmentInfoResponse {
  totalInvested: number;
  eligibleToUninvest: number;
  estimatedNextPayout: number;
  nextPayoutTime: string;
  dailyCreditsPerToken: number;
  tranches: {
    amount: number;
    investedAt: string;
    lastPayoutAt: string | null;
    isEligibleToUninvest: boolean;
  }[];
}

interface InvestResponse {
  newInvestedTotal: number;
  newAvailableTokens: number;
  trancheCount: number;
}

interface UninvestResponse {
  tokensUninvested: number;
  newInvestedTotal: number;
  newAvailableTokens: number;
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
        "Check your token balance, staking status, investment status, and current API rate limit. " +
        "Returns suggestions for staking (rate limits) and investing (passive credits).",
    },
    async () => {
      const suggestions: string[] = [];

      const [balanceResult, stakeResult, investResult] = await Promise.all([
        client.get<BalanceResponse>("/api/tokens/balance"),
        client.get<StakeInfoResponse>("/api/User/api-stake-info"),
        client.get<InvestmentInfoResponse>("/api/user/investment-info"),
      ]);

      if (!balanceResult.ok) return formatToolError(balanceResult);
      if (!stakeResult.ok) return formatToolError(stakeResult);

      const balance = balanceResult.data;
      const stake = stakeResult.data;

      // Investment info is optional -- if the endpoint returns 404 or any error
      // (e.g., API not yet deployed with investment feature), gracefully degrade
      // by showing zero investment data. Balance and stake are required.
      // NOTE: dailyCreditsPerToken comes from the API (canonical source is the grain constant).
      // If investment-info is unavailable, fall back to 100 as a reasonable default.
      const hasInvestment = investResult.ok;
      const investment = hasInvestment ? investResult.data : null;
      const investedTokens = investment?.totalInvested ?? 0;
      const dailyCreditsPerToken = investment?.dailyCreditsPerToken ?? 100;
      const dailyYield = investedTokens * dailyCreditsPerToken;
      const eligibleToUninvest = investment?.eligibleToUninvest ?? 0;

      if (balance.tokens === 0 && balance.stakedTokens === 0 && investedTokens === 0) {
        suggestions.push(
          `No tokens yet. Purchase tokens at ${TOKEN_PURCHASE_URL} ($10 each).`
        );
      } else {
        if (balance.availableTokens > 0 && stake.rateLimit < 100) {
          suggestions.push(
            `You have ${balance.availableTokens} unstaked tokens. Use psecs_stake_tokens to boost your rate limit.`
          );
        }

        if (balance.availableTokens > 0 && investedTokens === 0) {
          suggestions.push(
            `You have tokens available to invest for passive credits (${dailyCreditsPerToken} credits/day per token). Use psecs_invest_tokens.`
          );
        }

        if (investedTokens > 0) {
          suggestions.push(
            `Earning ${dailyYield.toLocaleString()} credits/day from ${investedTokens} invested token(s).`
          );
        }

        if (eligibleToUninvest > 0) {
          suggestions.push(
            `${eligibleToUninvest} invested token(s) are eligible to uninvest.`
          );
        }
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
        investedTokens,
        availableTokens: balance.availableTokens,
        currentRateLimit: stake.rateLimit,
        cooldownEndsAt: stake.cooldownEndsAt,
        ...(investment
          ? {
              dailyYield,
              nextPayoutTime: investment.nextPayoutTime,
              eligibleToUninvest: investment.eligibleToUninvest,
              estimatedNextPayout: investment.estimatedNextPayout,
            }
          : {}),
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

  server.registerTool(
    "psecs_invest_tokens",
    {
      description:
        "Invest tokens to earn 100 credits/day per token. Invested tokens are NOT consumed " +
        "and do not decay — they can be uninvested after the first daily payout. " +
        "Credits are deposited to your corp at midnight ET.",
      inputSchema: {
        amount: z
          .number()
          .min(0.1)
          .describe("Amount of tokens to invest (min 0.1)"),
      },
    },
    async (args) => {
      const result = await client.post<InvestResponse>(
        "/api/user/invest-tokens",
        { amount: args.amount }
      );
      if (!result.ok) return formatToolError(result);

      const data = result.data;
      // Canonical rate lives in the grain (DAILY_CREDIT_RATE_PER_TOKEN = 100).
      // psecs_token_status reads it from the investment-info endpoint; here we use
      // the constant directly since the invest response doesn't include it.
      const dailyYield = data.newInvestedTotal * 100;

      return formatToolResult({
        investedTokens: data.newInvestedTotal,
        availableTokens: data.newAvailableTokens,
        trancheCount: data.trancheCount,
        dailyYield,
        suggestions: [
          `Now earning ${dailyYield.toLocaleString()} credits/day. Next payout at midnight ET.`,
        ],
      });
    }
  );

  server.registerTool(
    "psecs_uninvest_tokens",
    {
      description:
        "Uninvest tokens to return them to your available balance. " +
        "Uses FIFO — oldest investments are uninvested first. " +
        "Tokens must have received at least one payout before they can be uninvested.",
      inputSchema: {
        amount: z
          .number()
          .min(0.01)
          .describe(
            "Amount of tokens to uninvest (no minimum — partial tranche remnants may be less than 0.1)"
          ),
      },
    },
    async (args) => {
      const result = await client.post<UninvestResponse>(
        "/api/user/uninvest-tokens",
        { amount: args.amount }
      );
      if (!result.ok) return formatToolError(result);

      const data = result.data;

      return formatToolResult({
        tokensUninvested: data.tokensUninvested,
        investedTokens: data.newInvestedTotal,
        availableTokens: data.newAvailableTokens,
      });
    }
  );
}
