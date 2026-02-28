import { describe, it, expect, beforeEach, afterEach, vi } from "vitest";
import { PsecsClient } from "./client.js";
import type { PsecsConfig } from "./config.js";

describe("PsecsClient", () => {
  const config: PsecsConfig = {
    apiKey: "test-api-key-abc123",
    baseUrl: "https://api.psecs.io",
  };

  let client: PsecsClient;
  let fetchMock: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchMock = vi.fn();
    vi.stubGlobal("fetch", fetchMock);
    client = new PsecsClient(config);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("sends GET request with auth header", async () => {
    fetchMock.mockResolvedValueOnce(
      new Response(JSON.stringify({ id: "corp-1", name: "TestCorp" }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      })
    );

    const result = await client.get<{ id: string; name: string }>("/corp");

    expect(fetchMock).toHaveBeenCalledOnce();
    const [url, init] = fetchMock.mock.calls[0];
    expect(url).toBe("https://api.psecs.io/corp");
    expect(init.method).toBe("GET");
    expect(init.headers["Authorization"]).toBe("Bearer test-api-key-abc123");
    expect(result).toEqual({
      ok: true,
      data: { id: "corp-1", name: "TestCorp" },
      status: 200,
    });
  });

  it("sends POST request with JSON body", async () => {
    fetchMock.mockResolvedValueOnce(
      new Response(JSON.stringify({ success: true }), {
        status: 201,
        headers: { "Content-Type": "application/json" },
      })
    );

    const body = { name: "NewCorp" };
    const result = await client.post<{ success: boolean }>("/corp", body);

    expect(fetchMock).toHaveBeenCalledOnce();
    const [url, init] = fetchMock.mock.calls[0];
    expect(url).toBe("https://api.psecs.io/corp");
    expect(init.method).toBe("POST");
    expect(init.headers["Authorization"]).toBe("Bearer test-api-key-abc123");
    expect(init.headers["Content-Type"]).toBe("application/json");
    expect(init.body).toBe(JSON.stringify(body));
    expect(result).toEqual({
      ok: true,
      data: { success: true },
      status: 201,
    });
  });

  it("classifies 401 as auth error", async () => {
    fetchMock.mockResolvedValueOnce(
      new Response(JSON.stringify({ message: "Invalid API key" }), {
        status: 401,
        headers: { "Content-Type": "application/json" },
      })
    );

    const result = await client.get("/corp");

    expect(result).toEqual({
      ok: false,
      errorType: "auth",
      message: "Invalid API key",
      status: 401,
    });
  });

  it("classifies 403 as game logic error, not auth", async () => {
    // 403 = AccessDeniedDomainException (grain-level access control), not an API key failure
    fetchMock.mockResolvedValueOnce(
      new Response(
        JSON.stringify({ errorMessage: "Owner access required" }),
        {
          status: 403,
          headers: { "Content-Type": "application/json" },
        }
      )
    );

    const result = await client.get("/some-endpoint");

    expect(result).toEqual({
      ok: false,
      errorType: "game",
      message: "Owner access required",
      status: 403,
    });
  });

  it("classifies 400/409/422 as game logic errors", async () => {
    for (const status of [400, 409, 422]) {
      fetchMock.mockResolvedValueOnce(
        new Response(
          JSON.stringify({ errorMessage: `Game error at ${status}` }),
          {
            status,
            headers: { "Content-Type": "application/json" },
          }
        )
      );

      const result = await client.get("/some-endpoint");

      expect(result).toEqual({
        ok: false,
        errorType: "game",
        message: `Game error at ${status}`,
        status,
      });
    }
  });

  it("classifies 429 as rate limit error with retryAfter", async () => {
    // The client auto-retries on 429 up to 3 times. Mock 4 responses (initial +
    // 3 retries) all returning 429, and use fake timers to skip the retry delays.
    vi.useFakeTimers();

    const rateLimitResponse = () =>
      new Response(JSON.stringify({ message: "Too many requests" }), {
        status: 429,
        headers: {
          "Content-Type": "application/json",
          "Retry-After": "30",
        },
      });

    fetchMock
      .mockResolvedValueOnce(rateLimitResponse())
      .mockResolvedValueOnce(rateLimitResponse())
      .mockResolvedValueOnce(rateLimitResponse())
      .mockResolvedValueOnce(rateLimitResponse());

    const resultPromise = client.get("/corp");
    await vi.runAllTimersAsync();
    const result = await resultPromise;

    vi.useRealTimers();

    expect(result).toEqual({
      ok: false,
      errorType: "rate_limit",
      message: "Too many requests",
      status: 429,
      retryAfter: 30,
    });
  });

  it("extracts DomainExceptionMessage from Orleans grain exceptions", async () => {
    // Orleans grain exceptions serialize with PascalCase DomainExceptionMessage,
    // not the lowercase 'message' or 'errorMessage' fields.
    fetchMock.mockResolvedValueOnce(
      new Response(
        JSON.stringify({
          EntityId: "aabbccdd-0000-0000-0000-000000000000",
          StringEntityId: null,
          DomainExceptionType: "FleetNotFoundDomainException",
          DomainExceptionMessage: "Fleet not found",
        }),
        {
          status: 400,
          headers: { "Content-Type": "application/json" },
        }
      )
    );

    const result = await client.get("/fleet/aabbccdd");

    expect(result).toEqual({
      ok: false,
      errorType: "game",
      message: "Fleet not found",
      status: 400,
    });
  });

  it("classifies 500 as infrastructure error", async () => {
    fetchMock.mockResolvedValueOnce(
      new Response(JSON.stringify({ message: "Internal server error" }), {
        status: 500,
        headers: { "Content-Type": "application/json" },
      })
    );

    const result = await client.get("/corp");

    expect(result).toEqual({
      ok: false,
      errorType: "infrastructure",
      message: "Internal server error",
      status: 500,
    });
  });

  it("classifies network failures as infrastructure error", async () => {
    fetchMock.mockRejectedValueOnce(new Error("fetch failed"));

    const result = await client.get("/corp");

    expect(result).toEqual({
      ok: false,
      errorType: "infrastructure",
      message: "fetch failed",
      status: 0,
    });
  });

  it("builds query string from params", async () => {
    fetchMock.mockResolvedValueOnce(
      new Response(JSON.stringify([]), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      })
    );

    await client.get("/market/sales", {
      query: { region: "nexus", limit: 10, active: true, cursor: undefined },
    });

    expect(fetchMock).toHaveBeenCalledOnce();
    const [url] = fetchMock.mock.calls[0];
    expect(url).toBe(
      "https://api.psecs.io/market/sales?region=nexus&limit=10&active=true"
    );
  });

  it("substitutes path parameters", async () => {
    fetchMock.mockResolvedValueOnce(
      new Response(JSON.stringify({ id: "fleet-1" }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      })
    );

    await client.get("/corp/{corpId}/fleet/{fleetId}", {
      path: { corpId: "corp-abc", fleetId: "fleet-123" },
    });

    expect(fetchMock).toHaveBeenCalledOnce();
    const [url] = fetchMock.mock.calls[0];
    expect(url).toBe("https://api.psecs.io/corp/corp-abc/fleet/fleet-123");
  });
});
