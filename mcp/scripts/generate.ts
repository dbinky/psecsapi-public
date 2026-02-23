/**
 * OpenAPI codegen script for PSECS MCP server.
 *
 * Reads openapi.json and produces:
 *   src/generated/types.ts   — TypeScript interfaces from OpenAPI schemas
 *   src/generated/raw-tools.ts — MCP tool registrations for every API endpoint
 *
 * Run: npx tsx scripts/generate.ts
 *
 * CONTRACT VERIFICATION NOTE:
 * The generated TypeScript types and hand-written tool response interfaces in
 * src/tools/*.ts can drift from the C# API response models. When C# response
 * model properties are renamed or restructured, the MCP TypeScript interfaces
 * must be updated manually. To detect drift, regenerate from a fresh
 * openapi.json and diff against the existing generated output:
 *   1. Start the API: dotnet run --project psecsapi.api
 *   2. Fetch spec: curl http://localhost:5130/swagger/v1/swagger.json -o openapi.json
 *   3. Regenerate: npx tsx scripts/generate.ts
 *   4. Diff: git diff src/generated/
 * Future improvement: add a CI step that performs this diff automatically.
 */

import * as fs from "node:fs";
import * as path from "node:path";

// ---------------------------------------------------------------------------
// Types for the subset of OpenAPI 3.0 we care about
// ---------------------------------------------------------------------------

interface OpenApiSpec {
  paths: Record<string, Record<string, OpenApiOperation>>;
  components: { schemas: Record<string, OpenApiSchema> };
}

interface OpenApiOperation {
  summary?: string;
  description?: string;
  tags?: string[];
  parameters?: OpenApiParameter[];
  requestBody?: {
    description?: string;
    content?: Record<string, { schema?: OpenApiSchema }>;
  };
  responses?: Record<string, unknown>;
}

interface OpenApiParameter {
  name: string;
  in: "path" | "query" | "header" | "cookie";
  description?: string;
  required?: boolean;
  schema?: OpenApiSchema;
}

interface OpenApiSchema {
  type?: string;
  format?: string;
  nullable?: boolean;
  enum?: string[];
  items?: OpenApiSchema;
  properties?: Record<string, OpenApiSchema>;
  additionalProperties?: boolean | OpenApiSchema;
  required?: string[];
  $ref?: string;
  allOf?: OpenApiSchema[];
  oneOf?: OpenApiSchema[];
  anyOf?: OpenApiSchema[];
  description?: string;
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

const ROOT = path.resolve(import.meta.dirname, "..");
const SPEC_PATH = path.join(ROOT, "openapi.json");
const OUT_DIR = path.join(ROOT, "src", "generated");

function ensureDir(dir: string) {
  if (!fs.existsSync(dir)) {
    fs.mkdirSync(dir, { recursive: true });
  }
}

/** Resolve a $ref like "#/components/schemas/Foo" to the schema name "Foo". */
function refName(ref: string): string {
  const parts = ref.split("/");
  return parts[parts.length - 1];
}

// ---------------------------------------------------------------------------
// Type generation
// ---------------------------------------------------------------------------

function schemaToTs(schema: OpenApiSchema, indent: string = ""): string {
  if (schema.$ref) {
    return refName(schema.$ref);
  }

  if (schema.allOf) {
    return schema.allOf.map((s) => schemaToTs(s, indent)).join(" & ");
  }
  if (schema.oneOf) {
    return schema.oneOf.map((s) => schemaToTs(s, indent)).join(" | ");
  }
  if (schema.anyOf) {
    return schema.anyOf.map((s) => schemaToTs(s, indent)).join(" | ");
  }

  if (schema.enum) {
    return schema.enum.map((v) => `"${v}"`).join(" | ");
  }

  if (schema.type === "array") {
    const itemType = schema.items ? schemaToTs(schema.items, indent) : "unknown";
    // Wrap union/intersection types in parens for array
    const needsParens = itemType.includes("|") || itemType.includes("&");
    return needsParens ? `(${itemType})[]` : `${itemType}[]`;
  }

  if (schema.type === "object" || schema.properties) {
    // Object with typed additionalProperties and no own properties = Record
    if (
      !schema.properties &&
      schema.additionalProperties &&
      typeof schema.additionalProperties === "object"
    ) {
      const valType = schemaToTs(schema.additionalProperties, indent);
      return `Record<string, ${valType}>`;
    }

    // Object with no properties at all
    if (!schema.properties) {
      return "Record<string, unknown>";
    }

    const requiredSet = new Set(schema.required ?? []);
    const lines: string[] = ["{"];
    const innerIndent = indent + "  ";

    for (const [propName, propSchema] of Object.entries(schema.properties)) {
      let propType = schemaToTs(propSchema, innerIndent);
      const isRequired = requiredSet.has(propName);
      const isNullable = propSchema.nullable === true;
      if (isNullable) {
        propType = `${propType} | null`;
      }
      const opt = isRequired ? "" : "?";
      lines.push(`${innerIndent}${safePropName(propName)}${opt}: ${propType};`);
    }

    // If additionalProperties is a schema, add index signature
    if (
      schema.additionalProperties &&
      typeof schema.additionalProperties === "object"
    ) {
      const valType = schemaToTs(schema.additionalProperties, innerIndent);
      lines.push(`${innerIndent}[key: string]: ${valType};`);
    }

    lines.push(`${indent}}`);
    return lines.join("\n");
  }

  // Primitive types
  switch (schema.type) {
    case "string":
      return "string";
    case "integer":
    case "number":
      return "number";
    case "boolean":
      return "boolean";
    default:
      return "unknown";
  }
}

function safePropName(name: string): string {
  // If the name is not a valid JS identifier, quote it
  if (/^[a-zA-Z_$][a-zA-Z0-9_$]*$/.test(name)) {
    return name;
  }
  return `"${name}"`;
}

function generateTypes(schemas: Record<string, OpenApiSchema>): string {
  const lines: string[] = [
    "// Auto-generated from openapi.json — do not edit by hand",
    "// Run: npm run generate",
    "",
  ];

  for (const [name, schema] of Object.entries(schemas)) {
    // Enums become type aliases
    if (schema.enum) {
      const members = schema.enum.map((v) => `"${v}"`).join(" | ");
      lines.push(`export type ${name} = ${members};`);
      lines.push("");
      continue;
    }

    // Objects become interfaces (if they produce a { ... } block) or type aliases
    if (schema.type === "object" || schema.properties) {
      const tsBody = schemaToTs(schema, "");
      if (tsBody.startsWith("{")) {
        lines.push(`export interface ${name} ${tsBody}`);
      } else {
        lines.push(`export type ${name} = ${tsBody};`);
      }
      lines.push("");
      continue;
    }

    // Anything else is a type alias
    const tsType = schemaToTs(schema, "");
    lines.push(`export type ${name} = ${tsType};`);
    lines.push("");
  }

  return lines.join("\n");
}

// ---------------------------------------------------------------------------
// Tool generation
// ---------------------------------------------------------------------------

/**
 * Derive a tool name from HTTP method + path.
 *
 * Examples:
 *   GET  /api/fleet/{fleetId}/scan/deep       -> psecs_raw_fleet_scan_deep
 *   POST /api/market                           -> psecs_raw_create_market
 *   DELETE /api/ship/{shipId}/extraction        -> psecs_raw_delete_ship_extraction
 *   POST /api/Ship/{shipId}/extraction          -> psecs_raw_create_ship_extraction
 *   GET  /api/Ship/{shipId}/extraction          -> psecs_raw_ship_extraction
 */
function deriveToolName(method: string, apiPath: string): string {
  // Strip /api/ prefix
  let stripped = apiPath.replace(/^\/api\//i, "");

  // Remove path parameter segments like {fleetId}
  stripped = stripped
    .split("/")
    .filter((seg) => !seg.startsWith("{"))
    .join("/");

  // Lowercase, replace / and - with _
  let name = stripped.toLowerCase().replace(/[\/-]/g, "_");

  // Collapse multiple underscores and trim
  name = name.replace(/_+/g, "_").replace(/^_|_$/g, "");

  // For non-GET methods, prepend action prefix
  const upperMethod = method.toUpperCase();
  if (upperMethod !== "GET") {
    const prefixMap: Record<string, string> = {
      POST: "create",
      PUT: "update",
      DELETE: "delete",
    };
    const prefix = prefixMap[upperMethod] ?? upperMethod.toLowerCase();
    name = `${prefix}_${name}`;
  }

  return `psecs_raw_${name}`;
}

/**
 * Map an OpenAPI parameter schema to a Zod type expression string.
 */
function paramToZod(param: OpenApiParameter): string {
  const schema = param.schema;
  if (!schema) return "z.string()";

  let zodType: string;
  switch (schema.type) {
    case "integer":
    case "number":
      // Query/path params arrive as strings; coerce to number
      zodType = "z.coerce.number()";
      break;
    case "boolean":
      zodType = "z.coerce.boolean()";
      break;
    default:
      zodType = "z.string()";
  }

  if (param.description) {
    zodType += `.describe(${JSON.stringify(param.description)})`;
  }

  if (!param.required) {
    zodType += ".optional()";
  }

  return zodType;
}

interface ToolDef {
  name: string;
  method: string;
  path: string;
  description: string;
  pathParams: OpenApiParameter[];
  queryParams: OpenApiParameter[];
  hasBody: boolean;
  bodyDescription?: string;
}

/**
 * Paths excluded from raw tool generation.
 * - Auth-destructive endpoints that could rotate or revoke the API key
 *   the MCP server is currently using, effectively bricking the session.
 * - Server-side/admin endpoints that players should never call.
 *
 * Audited 2026-02-21: all admin-only and auth-destructive endpoints are covered.
 * GET /api/Auth/api-key/status is intentionally allowed (read-only check).
 * POST /api/Space is intentionally allowed — player action that costs 0.01 tokens/sector
 * and adds sectors to the caller's personal map. Not an admin operation.
 */
const DENYLIST_PATHS: Array<{ path: string; method?: string }> = [
  { path: "/api/Auth/refresh" },
  { path: "/api/Auth/logout" },
  { path: "/api/Auth/api-key", method: "POST" },
  { path: "/api/Auth/api-key", method: "DELETE" },
  // Stripe webhook — server-to-server callback, not a player action
  { path: "/api/webhook/stripe" },
];

/**
 * Description suffixes appended to specific tool descriptions.
 * Used to communicate server-side constraints to AI agents.
 */
const DESCRIPTION_SUFFIXES: Array<{ path: string; method?: string; suffix: string }> = [
  {
    path: "/api/Space",
    method: "POST",
    suffix: " Body must be an integer between 1 and 100 (max sectors per request). Each sector costs 0.01 tokens.",
  },
];

function isDenylisted(apiPath: string, method: string): boolean {
  return DENYLIST_PATHS.some(
    (entry) =>
      entry.path === apiPath &&
      (entry.method === undefined || entry.method === method.toUpperCase())
  );
}

function getDescriptionSuffix(apiPath: string, method: string): string {
  const match = DESCRIPTION_SUFFIXES.find(
    (entry) =>
      entry.path === apiPath &&
      (entry.method === undefined || entry.method === method.toUpperCase())
  );
  return match?.suffix ?? "";
}

function collectTools(spec: OpenApiSpec): ToolDef[] {
  const tools: ToolDef[] = [];

  for (const [apiPath, methods] of Object.entries(spec.paths)) {
    for (const [method, operation] of Object.entries(methods)) {
      if (isDenylisted(apiPath, method)) continue;
      const params = operation.parameters ?? [];
      const pathParams = params.filter((p) => p.in === "path");
      const queryParams = params.filter((p) => p.in === "query");

      const hasBody = !!operation.requestBody;
      let bodyDescription: string | undefined;
      if (hasBody && operation.requestBody?.description) {
        bodyDescription = operation.requestBody.description;
      }

      const baseSummary =
        operation.summary ?? operation.description ?? `${method.toUpperCase()} ${apiPath}`;
      const description = baseSummary + getDescriptionSuffix(apiPath, method);

      tools.push({
        name: deriveToolName(method, apiPath),
        method: method.toUpperCase(),
        path: apiPath,
        description,
        pathParams,
        queryParams,
        hasBody,
        bodyDescription,
      });
    }
  }

  // Deduplicate names: when collisions exist, disambiguate using the trailing
  // path param name (e.g. {entryId} -> "_by_entry"). If a path ends in a param
  // segment, we derive a suffix from it.
  const nameCount = new Map<string, number>();
  for (const tool of tools) {
    nameCount.set(tool.name, (nameCount.get(tool.name) ?? 0) + 1);
  }

  for (const tool of tools) {
    if ((nameCount.get(tool.name) ?? 0) > 1) {
      // Check if this path has a trailing path param
      const segments = tool.path.split("/");
      const lastSeg = segments[segments.length - 1];
      if (lastSeg.startsWith("{")) {
        // Use the param name as a suffix: {entryId} -> "by_entry"
        const paramName = lastSeg.replace(/^\{|\}$/g, "");
        const hint = paramName.replace(/Id$/i, "").replace(/[A-Z]/g, (c) => `_${c.toLowerCase()}`).replace(/^_/, "").toLowerCase();
        tool.name = `${tool.name}_by_${hint}`;
      }
    }
  }

  // Final check for any remaining collisions — append _N
  const finalCount = new Map<string, number>();
  for (const tool of tools) {
    const count = finalCount.get(tool.name) ?? 0;
    if (count > 0) {
      tool.name = `${tool.name}_${count + 1}`;
    }
    finalCount.set(tool.name, count + 1);
  }

  return tools;
}

function generateRawTools(tools: ToolDef[]): string {
  const lines: string[] = [
    "// Auto-generated from openapi.json — do not edit by hand",
    "// Run: npm run generate",
    "",
    'import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";',
    'import { z } from "zod";',
    'import { PsecsClient } from "../client.js";',
    'import { formatToolResult, formatToolError } from "../tool-utils.js";',
    "",
    "export function registerRawTools(server: McpServer, client: PsecsClient): void {",
  ];

  for (const tool of tools) {
    lines.push("");
    lines.push(`  // ${tool.method} ${tool.path}`);

    // Build the inputSchema shape
    const schemaProps: string[] = [];

    for (const param of tool.pathParams) {
      schemaProps.push(`      ${param.name}: ${paramToZod(param)},`);
    }
    for (const param of tool.queryParams) {
      schemaProps.push(`      ${param.name}: ${paramToZod(param)},`);
    }
    if (tool.hasBody) {
      const bodyDesc = tool.bodyDescription
        ? `.describe(${JSON.stringify(tool.bodyDescription + " (JSON string)")})`
        : '.describe("Request body as JSON string")';
      schemaProps.push(`      body: z.string()${bodyDesc},`);
    }

    const hasInputSchema = schemaProps.length > 0;

    // Build the registerTool call
    lines.push(`  server.registerTool(`);
    lines.push(`    ${JSON.stringify(tool.name)},`);
    lines.push(`    {`);
    lines.push(`      description: ${JSON.stringify(tool.description)},`);
    if (hasInputSchema) {
      lines.push(`      inputSchema: {`);
      for (const prop of schemaProps) {
        lines.push(`  ${prop}`);
      }
      lines.push(`      },`);
    }
    lines.push(`    },`);

    // Build the handler
    if (hasInputSchema) {
      lines.push(`    async (args) => {`);
    } else {
      lines.push(`    async () => {`);
    }

    // Build path substitution options
    const hasPathParams = tool.pathParams.length > 0;
    const hasQueryParams = tool.queryParams.length > 0;

    if (hasPathParams || hasQueryParams) {
      lines.push(`      const options: { path?: Record<string, string>; query?: Record<string, string | number | boolean | undefined> } = {};`);
      if (hasPathParams) {
        lines.push(`      options.path = {`);
        for (const param of tool.pathParams) {
          lines.push(`        ${param.name}: String(args.${param.name}),`);
        }
        lines.push(`      };`);
      }
      if (hasQueryParams) {
        lines.push(`      options.query = {`);
        for (const param of tool.queryParams) {
          lines.push(`        ${param.name}: args.${param.name},`);
        }
        lines.push(`      };`);
      }
    }

    // Build the client call
    const optionsArg = hasPathParams || hasQueryParams ? "options" : "undefined";
    const clientMethod = tool.method.toLowerCase();

    switch (tool.method) {
      case "GET":
        lines.push(`      const result = await client.get(${JSON.stringify(tool.path)}, ${optionsArg});`);
        break;
      case "DELETE":
        lines.push(`      const result = await client.delete(${JSON.stringify(tool.path)}, ${optionsArg});`);
        break;
      case "POST":
        if (tool.hasBody) {
          lines.push(`      let parsedBody: unknown;`);
          lines.push(`      try { parsedBody = JSON.parse(args.body); } catch { return { isError: true, content: [{ type: "text" as const, text: "Invalid JSON in request body" }] }; }`);
          lines.push(`      const result = await client.post(${JSON.stringify(tool.path)}, parsedBody, ${optionsArg});`);
        } else {
          lines.push(`      const result = await client.post(${JSON.stringify(tool.path)}, undefined, ${optionsArg});`);
        }
        break;
      case "PUT":
        if (tool.hasBody) {
          lines.push(`      let parsedBody: unknown;`);
          lines.push(`      try { parsedBody = JSON.parse(args.body); } catch { return { isError: true, content: [{ type: "text" as const, text: "Invalid JSON in request body" }] }; }`);
          lines.push(`      const result = await client.put(${JSON.stringify(tool.path)}, parsedBody, ${optionsArg});`);
        } else {
          lines.push(`      const result = await client.put(${JSON.stringify(tool.path)}, undefined, ${optionsArg});`);
        }
        break;
      default:
        // Fallback — use the method name dynamically
        lines.push(`      const result = await client.${clientMethod}(${JSON.stringify(tool.path)}, ${optionsArg});`);
    }

    lines.push(`      if (!result.ok) return formatToolError(result);`);
    lines.push(`      return formatToolResult(result.data);`);
    lines.push(`    },`);
    lines.push(`  );`);
  }

  lines.push("}");
  lines.push("");

  return lines.join("\n");
}

// ---------------------------------------------------------------------------
// Main
// ---------------------------------------------------------------------------

function main() {
  const specText = fs.readFileSync(SPEC_PATH, "utf-8");
  const spec: OpenApiSpec = JSON.parse(specText);

  ensureDir(OUT_DIR);

  // Generate types
  const typesCode = generateTypes(spec.components.schemas);
  const typesPath = path.join(OUT_DIR, "types.ts");
  fs.writeFileSync(typesPath, typesCode);
  const schemaCount = Object.keys(spec.components.schemas).length;
  console.log(`Generated ${typesPath} (${schemaCount} types)`);

  // Generate tools
  const tools = collectTools(spec);
  const toolsCode = generateRawTools(tools);
  const toolsPath = path.join(OUT_DIR, "raw-tools.ts");
  fs.writeFileSync(toolsPath, toolsCode);
  console.log(`Generated ${toolsPath} (${tools.length} tools)`);
}

main();
