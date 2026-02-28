import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from "zod";
import { PsecsClient } from "../client.js";
import { formatToolResult, formatToolError } from "../tool-utils.js";

interface ManufacturingJob {
  jobId: string;
  shipId: string;
  shipName?: string;
  blueprintId?: string;
  blueprintQuality?: number;
  targetQuantity?: number;
  completedCount?: number;
  currentItemProgressPercent?: number;
  status?: string;
  displayName?: string;
  outputName?: string;
  outputType?: string;
  estimatedCompletion?: string;
  autoResume?: boolean;
  [key: string]: unknown;
}

interface ManufacturingStatusResponse {
  jobs?: ManufacturingJob[];
  totalActive?: number;
  totalPaused?: number;
  [key: string]: unknown;
}

interface OwnedBlueprint {
  instanceId: string;
  blueprintDefinitionId?: string;
  applicationId?: string;
  quality?: number;
  acquiredAt?: string;
  outputType?: string;
  outputName?: string;
  [key: string]: unknown;
}

interface BlueprintDetail {
  blueprintId?: string;
  outputType?: string;
  outputId?: string;
  baseWorkUnits?: number;
  inputResources?: Array<{
    label?: string;
    qualifier?: string;
    value?: string;
    quantity?: number;
    inputKind?: string;
    [key: string]: unknown;
  }>;
  inputComponents?: Array<{
    label?: string;
    componentType?: string;
    quantity?: number;
    [key: string]: unknown;
  }>;
  qualityProperties?: string[];
  capabilities?: Array<{
    type?: string;
    baseValue?: number;
    qualitySource?: string;
    [key: string]: unknown;
  }>;
  [key: string]: unknown;
}

interface CargoHoldSummary {
  items?: Array<{
    boxedAssetId?: string;
    name?: string;
    quantity?: number;
    [key: string]: unknown;
  }>;
  [key: string]: unknown;
}

interface ManufacturingStartResult {
  success?: boolean;
  jobId?: string;
  status?: string;
  nextTickAt?: string;
  errorMessage?: string;
  [key: string]: unknown;
}

export function registerManufacturingTools(
  server: McpServer,
  client: PsecsClient
): void {
  server.registerTool(
    "psecs_manufacturing_overview",
    {
      description:
        "Get a comprehensive manufacturing overview including active/paused jobs and owned blueprints. " +
        "Returns combined view with suggestions about idle capacity, paused jobs, and available blueprints.",
    },
    async () => {
      const suggestions: string[] = [];
      const warnings: string[] = [];

      // Parallel fetch manufacturing status and blueprints
      const [statusResult, blueprintsResult] = await Promise.all([
        client.get<ManufacturingStatusResponse>("/api/Manufacturing/status"),
        client.get<OwnedBlueprint[]>("/api/Manufacturing/blueprints"),
      ]);

      const status = statusResult.ok ? statusResult.data : null;
      const blueprints = blueprintsResult.ok ? blueprintsResult.data : null;

      if (!statusResult.ok) warnings.push("Could not fetch manufacturing status.");
      if (!blueprintsResult.ok) warnings.push("Could not fetch blueprint list.");

      // Generate suggestions based on jobs
      if (status) {
        const jobs = status.jobs ?? [];
        const activeJobs = jobs.filter((j) => j.status === "Active");
        const pausedJobs = jobs.filter(
          (j) => j.status !== "Active" && j.status !== "Completed" && j.status !== "Cancelled"
        );

        if (activeJobs.length === 0 && pausedJobs.length === 0) {
          suggestions.push(
            "No active manufacturing jobs. Use psecs_start_manufacturing to begin producing items from your blueprints."
          );
        }

        if (activeJobs.length > 0) {
          suggestions.push(
            `${activeJobs.length} active job(s) in progress. Use psecs_manufacturing_status for detailed progress.`
          );
        }

        if (pausedJobs.length > 0) {
          const pauseReasons = pausedJobs.map(
            (j) => `"${j.outputName ?? j.displayName ?? j.jobId}" (${j.status})`
          );
          warnings.push(
            `${pausedJobs.length} paused/stalled job(s): ${pauseReasons.join(", ")}. Check resources and cargo space.`
          );
          suggestions.push(
            "Paused jobs may need resources restocked or cargo space freed. Use psecs_raw_create_manufacturing_resume to resume."
          );
        }
      }

      // Generate suggestions based on blueprints
      if (blueprints) {
        if (blueprints.length === 0) {
          suggestions.push(
            "No blueprints owned yet. Research blueprint applications in the tech tree to unlock manufacturing."
          );
        } else {
          const byType = new Map<string, number>();
          for (const bp of blueprints) {
            const t = bp.outputType ?? "Unknown";
            byType.set(t, (byType.get(t) ?? 0) + 1);
          }
          const typeSummary = [...byType.entries()]
            .map(([type, count]) => `${type}: ${count}`)
            .join(", ");
          suggestions.push(
            `${blueprints.length} blueprint(s) available (${typeSummary}). Use psecs_start_manufacturing to begin production.`
          );
        }
      }

      return formatToolResult({
        status,
        blueprints,
        suggestions,
        warnings,
      });
    }
  );

  server.registerTool(
    "psecs_start_manufacturing",
    {
      description:
        "Start a manufacturing job on a ship. Input resources and components are auto-selected " +
        "from the ship's cargo hold — inputs must be in ship cargo before starting. " +
        "If inputs are currently in the Nexus Warehouse, use psecs_warehouse_withdraw first. " +
        "When blueprintDefinitionId is provided, fetches blueprint input requirements and available cargo before starting. " +
        "Returns job result with resource availability context.",
      inputSchema: {
        shipId: z.string().describe("Ship ID to manufacture on"),
        blueprintInstanceId: z
          .string()
          .describe("Blueprint instance ID (from psecs_manufacturing_overview instanceId field) — used to start the job"),
        blueprintDefinitionId: z
          .string()
          .optional()
          .describe("Blueprint definition ID (from psecs_manufacturing_overview blueprintDefinitionId field) — enables pre-flight resource requirements check"),
        quantity: z
          .number()
          .min(1)
          .default(1)
          .optional()
          .describe("Number of items to manufacture (default 1)"),
        displayName: z
          .string()
          .optional()
          .describe("Optional display name for the output item"),
        autoResume: z
          .boolean()
          .default(true)
          .optional()
          .describe("Auto-resume when resources/space become available (default true)"),
      },
    },
    async (args) => {
      const suggestions: string[] = [];
      const warnings: string[] = [];

      // Fetch cargo (always); fetch blueprint details only when definition ID is provided.
      // blueprintInstanceId is a GUID — it cannot be used with the blueprint detail endpoint,
      // which searches by tech tree definition ID (e.g. "bp_culture_vat").
      const [blueprintResult, cargoResult] = await Promise.all([
        args.blueprintDefinitionId
          ? client.get<BlueprintDetail>(
              "/api/Manufacturing/blueprint/{blueprintId}",
              { path: { blueprintId: args.blueprintDefinitionId } }
            )
          : Promise.resolve(null),
        client.get<CargoHoldSummary>("/api/Ship/{shipId}/cargo", {
          path: { shipId: args.shipId },
        }),
      ]);

      const blueprint = blueprintResult?.ok ? blueprintResult.data : null;
      const cargo = cargoResult.ok ? cargoResult.data : null;

      if (args.blueprintDefinitionId && !blueprintResult?.ok)
        warnings.push("Could not fetch blueprint details — proceeding with build attempt.");
      if (!cargoResult.ok)
        warnings.push("Could not fetch cargo — unable to verify resource availability.");

      // Provide context about inputs needed
      if (blueprint) {
        const inputResources = blueprint.inputResources ?? [];
        const inputComponents = blueprint.inputComponents ?? [];
        if (inputResources.length > 0 || inputComponents.length > 0) {
          const inputSummary = [
            ...inputResources.map(
              (r) => `${r.quantity ?? "?"} x ${r.label ?? r.qualifier ?? "resource"}`
            ),
            ...inputComponents.map(
              (c) => `${c.quantity ?? "?"} x ${c.label ?? c.componentType ?? "component"}`
            ),
          ].join(", ");
          suggestions.push(`Blueprint requires: ${inputSummary}`);
        }
      }

      // Start the manufacturing job
      const body: Record<string, unknown> = {
        shipId: args.shipId,
        blueprintInstanceId: args.blueprintInstanceId,
        quantity: args.quantity ?? 1,
        autoResume: args.autoResume ?? true,
      };
      if (args.displayName) {
        body.displayName = args.displayName;
      }

      const result = await client.post<ManufacturingStartResult>(
        "/api/Manufacturing/start",
        body
      );
      if (!result.ok) return formatToolError(result);

      const startData = result.data;
      if (startData.success === false) {
        warnings.push(startData.errorMessage ?? "Job failed to start.");
        return formatToolResult({
          result: startData,
          blueprintDetails: blueprint,
          cargoSummary: cargo,
          suggestions,
          warnings,
        });
      }

      suggestions.push(
        "Manufacturing job started. Use psecs_manufacturing_status to monitor progress."
      );
      if (startData.nextTickAt) {
        suggestions.push(`Next processing tick at: ${startData.nextTickAt}`);
      }
      if (args.autoResume ?? true) {
        suggestions.push(
          "Auto-resume is enabled — the job will pause and auto-resume if resources run out or cargo is full."
        );
      }

      return formatToolResult({
        result: startData,
        blueprintDetails: blueprint,
        cargoSummary: cargo,
        suggestions,
        warnings,
      });
    }
  );

  server.registerTool(
    "psecs_manufacturing_status",
    {
      description:
        "Get detailed manufacturing job status, optionally filtered by ship. " +
        "Shows progress, paused/stalled jobs, and estimated completion times.",
      inputSchema: {
        shipId: z
          .string()
          .optional()
          .describe("Optional ship ID to filter jobs to a single ship"),
      },
    },
    async (args) => {
      const suggestions: string[] = [];
      const warnings: string[] = [];

      const queryOpts: { query?: Record<string, string | number | boolean | undefined> } = {};
      if (args.shipId) {
        queryOpts.query = { shipId: args.shipId };
      }

      const statusResult = await client.get<ManufacturingStatusResponse>(
        "/api/Manufacturing/status",
        queryOpts
      );
      if (!statusResult.ok) return formatToolError(statusResult);

      const status = statusResult.data;
      const jobs = status.jobs ?? [];

      if (jobs.length === 0) {
        suggestions.push(
          args.shipId
            ? "No manufacturing jobs on this ship. Use psecs_start_manufacturing to begin production."
            : "No manufacturing jobs found. Use psecs_manufacturing_overview to see available blueprints."
        );
        return formatToolResult({ status, jobs: [], summary: { total: 0 }, suggestions, warnings });
      }

      // Categorize jobs — status strings match ManufacturingJobStatus enum names
      // Paused: user-paused or zero-capacity; WaitingResources: no inputs; WaitingSpace: cargo full
      const active = jobs.filter((j) => j.status === "Active");
      const paused = jobs.filter((j) => j.status === "Paused");
      const pausedResources = jobs.filter((j) => j.status === "WaitingResources");
      const pausedCargo = jobs.filter((j) => j.status === "WaitingSpace");
      const completed = jobs.filter((j) => j.status === "Completed");

      // Generate suggestions for paused/stalled jobs
      if (pausedResources.length > 0) {
        const names = pausedResources.map(
          (j) => j.outputName ?? j.displayName ?? j.jobId
        );
        warnings.push(
          `${pausedResources.length} job(s) paused — insufficient resources: ${names.join(", ")}. ` +
            "Mine more resources or transfer from another ship."
        );
      }

      if (pausedCargo.length > 0) {
        const names = pausedCargo.map(
          (j) => j.outputName ?? j.displayName ?? j.jobId
        );
        warnings.push(
          `${pausedCargo.length} job(s) paused — cargo full: ${names.join(", ")}. ` +
            "Free up cargo space: sell items with psecs_market_sell or transfer to another ship."
        );
      }

      if (paused.length > 0) {
        suggestions.push(
          `${paused.length} job(s) paused by user. Use psecs_raw_create_manufacturing_resume to resume.`
        );
      }

      if (active.length > 0) {
        // Find job closest to completion
        const nearComplete = [...active]
          .filter((j) => j.currentItemProgressPercent !== undefined)
          .sort(
            (a, b) =>
              (b.currentItemProgressPercent ?? 0) - (a.currentItemProgressPercent ?? 0)
          );
        if (nearComplete.length > 0) {
          const best = nearComplete[0];
          suggestions.push(
            `Nearest completion: "${best.outputName ?? best.displayName ?? best.jobId}" at ${best.currentItemProgressPercent}% ` +
              `(${best.completedCount ?? 0}/${best.targetQuantity ?? "?"} items done).`
          );
        }
      }

      if (completed.length > 0) {
        suggestions.push(
          `${completed.length} job(s) completed. Finished items are in ship cargo — use psecs_ship_cargo_overview to view them.`
        );
      }

      return formatToolResult({
        status,
        summary: {
          total: jobs.length,
          active: active.length,
          paused: paused.length,
          waitingResources: pausedResources.length,
          waitingSpace: pausedCargo.length,
          completed: completed.length,
        },
        suggestions,
        warnings,
      });
    }
  );
}
