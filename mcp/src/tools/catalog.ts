import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { PsecsClient } from "../client.js";
import { formatToolResult, formatToolError } from "../tool-utils.js";

interface UserProfile {
  entityId?: string;
  ownedCorps?: string[];
  [key: string]: unknown;
}

interface CatalogEntry {
  entryId?: string;
  rawResourceId?: string;
  name?: string;
  shortNameKey?: string;
  group?: string;
  type?: string;
  class?: string;
  order?: string;
  properties?: Record<string, number | null>;
  density?: number;
  sectorId?: string;
  sectorName?: string;
  orbitalPosition?: number | null;
  discoveredAt?: string;
  discoveredByUserId?: string;
  isFavorite?: boolean;
  note?: string | null;
  [key: string]: unknown;
}

export function registerCatalogTools(
  server: McpServer,
  client: PsecsClient
): void {
  server.registerTool(
    "psecs_catalog_list",
    {
      description:
        "List all discovered resources in your corp's catalog, sorted by density descending. " +
        "The catalog is your persistent discovery log — every resource found via deep scan is recorded here " +
        "with its sector location, orbital position, density, and quality properties. " +
        "Filter by type (Mineral/Chemical/Flora/Fauna/Microscopic), class (Metal/Ore/Gas/Gemstone/Liquid/Food/etc.), " +
        "or show only favorited entries. " +
        "Density drives extraction rate: rate = ship_capability × density × (1 + modifier%). " +
        "Use this to find the best extraction targets and plan fleet routes.",
      inputSchema: {
        type: z
          .enum(["Mineral", "Chemical", "Flora", "Fauna", "Microscopic"])
          .optional()
          .describe("Filter by resource type"),
        class: z
          .string()
          .optional()
          .describe(
            "Filter by resource class (Metal, Ore, Gas, Gemstone, Liquid, Food, etc.)"
          ),
        favoritesOnly: z
          .boolean()
          .optional()
          .describe("If true, only return favorited entries"),
      },
    },
    async (args) => {
      const suggestions: string[] = [];
      const warnings: string[] = [];

      const userResult = await client.get<UserProfile>("/api/User");
      if (!userResult.ok) return formatToolError(userResult);
      const user = userResult.data;
      if (!user.ownedCorps || user.ownedCorps.length === 0) {
        return formatToolResult({
          entries: [],
          warnings: ["No corporation found. Create a corp first."],
          suggestions: ["Use psecs_create_corp to start playing."],
        });
      }
      const corpId = user.ownedCorps[0];

      const query: Record<string, string | boolean | undefined> = {};
      if (args.type) query.type = args.type;
      if (args.class) query.resourceClass = args.class;
      if (args.favoritesOnly) query.favoritesOnly = args.favoritesOnly;

      const catalogResult = await client.get<CatalogEntry[]>(
        "/api/corp/{corpId}/catalog",
        { path: { corpId }, query }
      );
      if (!catalogResult.ok) return formatToolError(catalogResult);

      const entries = catalogResult.data;
      if (entries.length === 0) {
        warnings.push("No resources in catalog.");
        suggestions.push(
          "Use psecs_deep_scan with sensor level 3 to discover resources and populate your catalog."
        );
        return formatToolResult({ count: 0, entries: [], warnings, suggestions });
      }

      // Sort by density descending — highest extraction rate potential first
      const sorted = [...entries].sort(
        (a, b) => (b.density ?? 0) - (a.density ?? 0)
      );

      const topEntry = sorted[0];
      if (topEntry.density && topEntry.density >= 0.7) {
        suggestions.push(
          `Your highest-density resource is "${topEntry.name}" (density ${topEntry.density.toFixed(3)}) ` +
            `at ${topEntry.sectorName ?? "unknown sector"}` +
            (topEntry.orbitalPosition != null
              ? ` orbital ${topEntry.orbitalPosition}`
              : "") +
            ". Navigate your fleet there for efficient extraction."
        );
      }

      if (!args.favoritesOnly) {
        const favoriteCount = entries.filter((e) => e.isFavorite).length;
        if (favoriteCount > 0) {
          suggestions.push(
            `You have ${favoriteCount} favorited entry/entries. Use favoritesOnly: true to filter to priority targets.`
          );
        } else {
          suggestions.push(
            "Use psecs_catalog_favorite to bookmark your best extraction targets for quick retrieval."
          );
        }
      }

      suggestions.push(
        "Use psecs_catalog_note to annotate entries with fleet assignments, quality observations, or extraction plans."
      );

      return formatToolResult({
        count: sorted.length,
        entries: sorted.map((e) => ({
          entryId: e.entryId,
          rawResourceId: e.rawResourceId,
          name: e.name,
          type: e.type,
          class: e.class,
          order: e.order,
          density: e.density,
          sectorId: e.sectorId,
          sectorName: e.sectorName,
          orbitalPosition: e.orbitalPosition,
          isFavorite: e.isFavorite,
          note: e.note,
          discoveredAt: e.discoveredAt,
        })),
        warnings,
        suggestions,
      });
    }
  );

  server.registerTool(
    "psecs_catalog_favorite",
    {
      description:
        "Add or remove a catalog entry from favorites. " +
        "Favorites are your priority extraction targets — bookmark the highest-density resources " +
        "in sectors near your regular routes. " +
        "Use psecs_catalog_list with favoritesOnly: true to see your prioritized targets.",
      inputSchema: {
        entryId: z
          .string()
          .describe("Catalog entry ID (from psecs_catalog_list)"),
        favorite: z
          .boolean()
          .describe("true to add to favorites, false to remove"),
      },
    },
    async (args) => {
      const suggestions: string[] = [];

      const userResult = await client.get<UserProfile>("/api/User");
      if (!userResult.ok) return formatToolError(userResult);
      const user = userResult.data;
      if (!user.ownedCorps || user.ownedCorps.length === 0) {
        return formatToolResult({ warnings: ["No corporation found."] });
      }
      const corpId = user.ownedCorps[0];

      const result = args.favorite
        ? await client.post<unknown>(
            "/api/corp/{corpId}/catalog/{entryId}/favorite",
            undefined,
            { path: { corpId, entryId: args.entryId } }
          )
        : await client.delete<unknown>(
            "/api/corp/{corpId}/catalog/{entryId}/favorite",
            { path: { corpId, entryId: args.entryId } }
          );
      if (!result.ok) return formatToolError(result);

      suggestions.push(
        args.favorite
          ? "Entry favorited. Use psecs_catalog_list with favoritesOnly: true to see all priority targets."
          : "Entry unfavorited."
      );

      return formatToolResult({
        action: args.favorite ? "favorited" : "unfavorited",
        entryId: args.entryId,
        suggestions,
      });
    }
  );

  server.registerTool(
    "psecs_catalog_note",
    {
      description:
        "Set or clear a personal note on a catalog entry. " +
        "Notes are ideal for tracking extraction assignments, quality observations, or route planning. " +
        "Examples: 'Assigned Fleet Alpha', 'Dense ore near nexus — high priority next run', 'Checked 2026-02 — still spawned'. " +
        "Pass null or empty string to clear the note. Maximum 500 characters.",
      inputSchema: {
        entryId: z
          .string()
          .describe("Catalog entry ID (from psecs_catalog_list)"),
        note: z
          .string()
          .nullable()
          .describe("Note content (max 500 characters), or null/empty to clear"),
      },
    },
    async (args) => {
      const suggestions: string[] = [];

      const userResult = await client.get<UserProfile>("/api/User");
      if (!userResult.ok) return formatToolError(userResult);
      const user = userResult.data;
      if (!user.ownedCorps || user.ownedCorps.length === 0) {
        return formatToolResult({ warnings: ["No corporation found."] });
      }
      const corpId = user.ownedCorps[0];

      const isClearing = !args.note || args.note.trim().length === 0;
      if (!isClearing && args.note!.length > 500) {
        return formatToolResult({
          warnings: [
            "Note exceeds 500 characters. Truncate to 500 characters and retry.",
          ],
        });
      }

      const result = isClearing
        ? await client.delete<unknown>(
            "/api/corp/{corpId}/catalog/{entryId}/note",
            { path: { corpId, entryId: args.entryId } }
          )
        : await client.put<unknown>(
            "/api/corp/{corpId}/catalog/{entryId}/note",
            { content: args.note },
            { path: { corpId, entryId: args.entryId } }
          );
      if (!result.ok) return formatToolError(result);

      suggestions.push(
        isClearing
          ? "Note cleared."
          : "Note saved. Use psecs_catalog_list to view catalog entries with their notes."
      );

      return formatToolResult({
        action: isClearing ? "note_cleared" : "note_set",
        entryId: args.entryId,
        note: isClearing ? null : args.note,
        suggestions,
      });
    }
  );
}
