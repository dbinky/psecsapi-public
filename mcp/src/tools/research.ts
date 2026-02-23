import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { PsecsClient } from "../client.js";
import { formatToolResult, formatToolError } from "../tool-utils.js";

interface ResearchStatus {
  totalCapacity?: number;
  totalAllocation?: number;
  activeProjects?: Array<{
    targetId: string;
    targetName?: string;
    targetType?: string;
    currentPoints?: number;
    requiredPoints?: number;
    allocationPercent?: number;
    nextTickAt?: string;
    startedAt?: string;
    [key: string]: unknown;
  }>;
  [key: string]: unknown;
}

interface ResearchListResponse {
  technologies?: Array<{
    id: string;
    name: string;
    tier?: number;
    primaryDiscipline?: string;
    secondaryDiscipline?: string;
    researchCost?: number;
    prerequisites?: string[];
    isResearched?: boolean;
    isVisible?: boolean;
    [key: string]: unknown;
  }>;
  applications?: Array<{
    id: string;
    technologyId?: string;
    name: string;
    type?: string;
    researchCost?: number;
    prerequisites?: string[];
    isResearched?: boolean;
    isVisible?: boolean;
    [key: string]: unknown;
  }>;
  [key: string]: unknown;
}

interface ResearchCompletedResponse {
  technologies?: string[];
  applications?: Array<{
    instanceId?: string;
    applicationId: string;
    name?: string;
    quality?: number;
    completedAt?: string;
    [key: string]: unknown;
  }>;
  [key: string]: unknown;
}

interface ResearchModifiers {
  modifiers?: Record<string, number>;
  [key: string]: unknown;
}

interface ResearchAllocateResult {
  success?: boolean;
  errorMessage?: string;
  project?: {
    targetId: string;
    targetName?: string;
    targetType?: string;
    currentPoints?: number;
    requiredPoints?: number;
    allocationPercent?: number;
    [key: string]: unknown;
  };
  [key: string]: unknown;
}

export function registerResearchTools(
  server: McpServer,
  client: PsecsClient
): void {
  server.registerTool(
    "psecs_research_overview",
    {
      description:
        "Get a comprehensive overview of the corporation's research state. " +
        "Fetches research status, available technologies, completed research, and active modifiers. " +
        "Returns combined view with strategic suggestions.",
    },
    async () => {
      const suggestions: string[] = [];
      const warnings: string[] = [];

      // Parallel fetch all research data
      const [statusResult, listResult, completedResult, modifiersResult] =
        await Promise.all([
          client.get<ResearchStatus>("/api/Research/status"),
          client.get<ResearchListResponse>("/api/Research/list"),
          client.get<ResearchCompletedResponse>("/api/Research/completed"),
          client.get<ResearchModifiers>("/api/Research/modifiers"),
        ]);

      const status = statusResult.ok ? statusResult.data : null;
      const list = listResult.ok ? listResult.data : null;
      const completed = completedResult.ok ? completedResult.data : null;
      const modifiers = modifiersResult.ok ? modifiersResult.data : null;

      if (!statusResult.ok) warnings.push("Could not fetch research status.");
      if (!listResult.ok) warnings.push("Could not fetch available research.");
      if (!completedResult.ok) warnings.push("Could not fetch completed research.");
      if (!modifiersResult.ok) warnings.push("Could not fetch research modifiers.");

      // Generate suggestions based on status
      if (status) {
        const totalCapacity = status.totalCapacity ?? 0;
        const totalAllocation = status.totalAllocation ?? 0;

        if (totalCapacity === 0) {
          suggestions.push(
            "No research capacity available. Install research modules on a ship to unlock the tech tree."
          );
        } else if (totalAllocation < 100) {
          const unallocatedPct = 100 - totalAllocation;
          suggestions.push(
            `Research capacity ${unallocatedPct}% unallocated. Use psecs_allocate_research to start new research projects.`
          );
        } else {
          suggestions.push(
            "Research capacity fully allocated. To research something new, use psecs_stop_research to free up capacity first."
          );
        }

        const activeProjects = status.activeProjects ?? [];
        if (activeProjects.length > 0) {
          // Find project closest to completion
          const closestToComplete = [...activeProjects]
            .filter((p) => p.requiredPoints && p.currentPoints !== undefined)
            .sort(
              (a, b) =>
                (b.currentPoints ?? 0) / (b.requiredPoints ?? 1) -
                (a.currentPoints ?? 0) / (a.requiredPoints ?? 1)
            );
          if (closestToComplete.length > 0) {
            const best = closestToComplete[0];
            const pct = Math.round(
              ((best.currentPoints ?? 0) / (best.requiredPoints ?? 1)) * 100
            );
            suggestions.push(
              `Closest to completion: "${best.targetName ?? best.targetId}" at ${pct}%.`
            );
          }
        }
      }

      // Count available technologies by tier
      if (list) {
        const visibleUnresearched = (list.technologies ?? []).filter(
          (t) => t.isVisible && !t.isResearched
        );
        if (visibleUnresearched.length > 0) {
          const byTier = new Map<number, number>();
          for (const tech of visibleUnresearched) {
            const tier = tech.tier ?? 0;
            byTier.set(tier, (byTier.get(tier) ?? 0) + 1);
          }
          const tierSummary = [...byTier.entries()]
            .sort((a, b) => a[0] - b[0])
            .map(([tier, count]) => `T${tier}: ${count}`)
            .join(", ");
          suggestions.push(
            `${visibleUnresearched.length} technology(ies) available to research (${tierSummary}).`
          );
        }
      }

      // Modifier summary
      if (modifiers) {
        const activeModifiers = Object.entries(modifiers.modifiers ?? {});
        if (activeModifiers.length > 0) {
          suggestions.push(
            `${activeModifiers.length} active research modifier(s) boosting your operations.`
          );
        } else {
          suggestions.push(
            "No research modifiers active. Research modifier applications to boost extraction, manufacturing, and research speed."
          );
        }
      }

      return formatToolResult({
        status,
        availableResearch: list,
        completedResearch: completed,
        modifiers,
        suggestions,
        warnings,
      });
    }
  );

  server.registerTool(
    "psecs_allocate_research",
    {
      description:
        "Allocate research capacity to a technology or application. " +
        "Starts or updates research on the target with the specified percentage. " +
        "Total allocation across all projects cannot exceed 100%.",
      inputSchema: {
        targetId: z
          .string()
          .describe("Technology or application ID to research"),
        percent: z
          .number()
          .int()
          .min(1)
          .max(100)
          .describe("Percentage of research capacity to allocate (1-100), must be a whole number"),
      },
    },
    async (args) => {
      const suggestions: string[] = [];
      const warnings: string[] = [];

      // Start/update the allocation
      const allocResult = await client.post<ResearchAllocateResult>(
        "/api/Research/allocate",
        { targetId: args.targetId, percent: args.percent }
      );
      if (!allocResult.ok) return formatToolError(allocResult);

      const allocData = allocResult.data;
      if (allocData.success === false) {
        warnings.push(allocData.errorMessage ?? "Allocation failed.");
        return formatToolResult({ result: allocData, suggestions, warnings });
      }

      // Fetch updated status for context
      const statusResult = await client.get<ResearchStatus>(
        "/api/Research/status"
      );
      const status = statusResult.ok ? statusResult.data : null;
      if (!statusResult.ok)
        warnings.push("Could not fetch updated research status.");

      // Generate suggestions
      if (status) {
        const totalCapacity = status.totalCapacity ?? 0;
        const totalAllocation = status.totalAllocation ?? 0;
        if (totalAllocation < 100) {
          const remaining = 100 - totalAllocation;
          suggestions.push(
            `${remaining}% capacity still unallocated. Consider starting additional research.`
          );
        } else {
          suggestions.push(
            "Research capacity fully allocated. Use psecs_stop_research to free up capacity if you want to change priorities."
          );
        }
      }

      suggestions.push(
        "Use psecs_research_overview to see all active projects and estimated completion."
      );

      return formatToolResult({
        result: allocData,
        updatedStatus: status,
        suggestions,
        warnings,
      });
    }
  );

  server.registerTool(
    "psecs_stop_research",
    {
      description:
        "Stop research on a technology or application, freeing the allocated capacity. " +
        "Accumulated progress is preserved — if you resume later, it picks up where it left off. " +
        "Use this to reallocate capacity to higher-priority targets.",
      inputSchema: {
        targetId: z
          .string()
          .describe("Technology or application ID to stop researching"),
      },
    },
    async (args) => {
      const suggestions: string[] = [];
      const warnings: string[] = [];

      // Stop the research
      const stopResult = await client.post(
        "/api/Research/stop",
        { targetId: args.targetId }
      );
      if (!stopResult.ok) return formatToolError(stopResult);

      suggestions.push(
        `Research on "${args.targetId}" stopped. Progress has been preserved.`
      );

      // Fetch updated status for context
      const statusResult = await client.get<ResearchStatus>(
        "/api/Research/status"
      );
      const status = statusResult.ok ? statusResult.data : null;
      if (!statusResult.ok)
        warnings.push("Could not fetch updated research status.");

      if (status) {
        const totalCapacity = status.totalCapacity ?? 0;
        const totalAllocation = status.totalAllocation ?? 0;

        if (totalAllocation < 100) {
          const freedPct = 100 - totalAllocation;
          suggestions.push(
            `${freedPct}% research capacity now available. Use psecs_tech_tree_path to plan which tech to prioritize, then psecs_allocate_research to start it.`
          );
        }

        const activeProjects = status.activeProjects ?? [];
        if (activeProjects.length === 0) {
          warnings.push(
            "No active research projects! Use psecs_allocate_research to avoid wasting research capacity."
          );
        }
      }

      suggestions.push(
        "Use psecs_research_overview to see updated research state and plan your next allocation."
      );

      return formatToolResult({
        stopped: args.targetId,
        updatedStatus: status,
        suggestions,
        warnings,
      });
    }
  );

  server.registerTool(
    "psecs_tech_tree_path",
    {
      description:
        "Analyze the path to a target technology or application. " +
        "Shows what prerequisites are needed, what's already completed, and what's missing. " +
        "Helps plan a research strategy to reach a specific goal.",
      inputSchema: {
        targetId: z
          .string()
          .describe("Technology or application ID to find a path to"),
      },
    },
    async (args) => {
      const suggestions: string[] = [];
      const warnings: string[] = [];

      // Fetch available and completed research in parallel
      const [listResult, completedResult] = await Promise.all([
        client.get<ResearchListResponse>("/api/Research/list"),
        client.get<ResearchCompletedResponse>("/api/Research/completed"),
      ]);

      if (!listResult.ok) return formatToolError(listResult);
      if (!completedResult.ok) return formatToolError(completedResult);

      const list = listResult.data;
      const completed = completedResult.data;

      const allTechs = list.technologies ?? [];
      const allApps = list.applications ?? [];
      const completedTechIds = new Set(completed.technologies ?? []);
      const completedAppIds = new Set(
        (completed.applications ?? []).map((a) => a.applicationId)
      );

      // Find the target — could be a tech or an application
      const targetTech = allTechs.find((t) => t.id === args.targetId);
      const targetApp = allApps.find((a) => a.id === args.targetId);

      if (!targetTech && !targetApp) {
        warnings.push(
          `Target "${args.targetId}" not found in the tech tree. Check the ID and try again.`
        );
        suggestions.push(
          "Use psecs_research_overview to see available technologies and applications."
        );
        return formatToolResult({ target: null, suggestions, warnings });
      }

      // Build prerequisite chain
      const missingPrereqs: Array<{
        id: string;
        name: string;
        type: string;
        tier?: number;
        isVisible?: boolean;
      }> = [];
      const completedPrereqs: Array<{ id: string; name: string; type: string }> = [];

      // Recursively gather prerequisites for a technology
      const visited = new Set<string>();
      function gatherTechPrereqs(techId: string): void {
        if (visited.has(techId)) return;
        visited.add(techId);

        const tech = allTechs.find((t) => t.id === techId);
        if (!tech) return;

        for (const prereqId of tech.prerequisites ?? []) {
          gatherTechPrereqs(prereqId);
          const prereqTech = allTechs.find((t) => t.id === prereqId);
          if (completedTechIds.has(prereqId)) {
            completedPrereqs.push({
              id: prereqId,
              name: prereqTech?.name ?? prereqId,
              type: "Technology",
            });
          } else {
            missingPrereqs.push({
              id: prereqId,
              name: prereqTech?.name ?? prereqId,
              type: "Technology",
              tier: prereqTech?.tier,
              isVisible: prereqTech?.isVisible,
            });
          }
        }
      }

      if (targetTech) {
        // Target is a technology
        gatherTechPrereqs(targetTech.id);

        if (completedTechIds.has(targetTech.id)) {
          suggestions.push(
            `"${targetTech.name}" is already researched! Use psecs_research_overview to see its applications, ` +
            `then psecs_allocate_research with an application ID to unlock blueprints and modifiers.`
          );
        } else if (missingPrereqs.length === 0) {
          if (targetTech.isVisible) {
            suggestions.push(
              `"${targetTech.name}" is ready to research — all prerequisites met. Use psecs_allocate_research to start.`
            );
          } else {
            suggestions.push(
              `"${targetTech.name}" has no missing prerequisites but is not yet visible. It may require additional progression.`
            );
          }
        } else {
          const visibleMissing = missingPrereqs.filter((p) => p.isVisible);
          suggestions.push(
            `${missingPrereqs.length} prerequisite(s) still needed to unlock "${targetTech.name}".`
          );
          if (visibleMissing.length > 0) {
            suggestions.push(
              `Start with: ${visibleMissing.map((p) => `"${p.name}"`).join(", ")} — these are visible and ready to research.`
            );
          }
        }
      } else if (targetApp) {
        // Target is an application — find its parent technology prereqs
        if (targetApp.technologyId) {
          gatherTechPrereqs(targetApp.technologyId);

          if (!completedTechIds.has(targetApp.technologyId)) {
            const parentTech = allTechs.find(
              (t) => t.id === targetApp.technologyId
            );
            missingPrereqs.push({
              id: targetApp.technologyId!,
              name: parentTech?.name ?? targetApp.technologyId!,
              type: "Technology",
              tier: parentTech?.tier,
              isVisible: parentTech?.isVisible,
            });
          }
        }

        // Check application-specific prerequisites
        for (const prereqId of targetApp.prerequisites ?? []) {
          if (completedAppIds.has(prereqId)) {
            const prereqApp = allApps.find((a) => a.id === prereqId);
            completedPrereqs.push({
              id: prereqId,
              name: prereqApp?.name ?? prereqId,
              type: "Application",
            });
          } else {
            const prereqApp = allApps.find((a) => a.id === prereqId);
            missingPrereqs.push({
              id: prereqId,
              name: prereqApp?.name ?? prereqId,
              type: "Application",
              isVisible: prereqApp?.isVisible,
            });
          }
        }

        if (completedAppIds.has(targetApp.id)) {
          suggestions.push(
            `"${targetApp.name}" is already researched!`
          );
        } else if (missingPrereqs.length === 0) {
          if (targetApp.isVisible) {
            suggestions.push(
              `"${targetApp.name}" is ready to research — all prerequisites met. Use psecs_allocate_research to start.`
            );
          } else {
            suggestions.push(
              `"${targetApp.name}" prerequisites are met but it is not yet visible.`
            );
          }
        } else {
          suggestions.push(
            `${missingPrereqs.length} prerequisite(s) needed to unlock "${targetApp.name}".`
          );
        }
      }

      const target = targetTech ?? targetApp;

      return formatToolResult({
        target: {
          id: target!.id,
          name: target!.name,
          type: targetTech ? "Technology" : "Application",
          isResearched: targetTech
            ? completedTechIds.has(target!.id)
            : completedAppIds.has(target!.id),
        },
        missingPrerequisites: missingPrereqs,
        completedPrerequisites: completedPrereqs,
        suggestions,
        warnings,
      });
    }
  );
}
