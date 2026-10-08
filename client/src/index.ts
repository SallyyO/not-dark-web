import { serve } from "bun";
import index from "./index.html";

const API_SERVER = "http://127.0.0.1:5015";

async function proxy(request: Request) {
  const incoming = new URL(request.url);
  const target = new URL(incoming.pathname + incoming.search, API_SERVER);

  try {
    const headers = new Headers(request.headers);
    headers.delete("host");

    return await fetch(target, {
      method: request.method,
      headers,
      body:
          request.method === "GET" || request.method === "HEAD"
              ? undefined
              : request.body,
      // @ts-expect-error Bun supports this at runtime.
      duplex:
          request.method === "GET" || request.method === "HEAD"
              ? undefined
              : "half",
    });
  } catch (error) {
    console.error(`API request failed: ${request.method} ${target}`);

    return new Response(
        JSON.stringify({
          error: "The .NET API is not reachable.",
          api: API_SERVER,
          details: error instanceof Error ? error.message : String(error),
        }),
        {
          status: 502,
          headers: {
            "Content-Type": "application/json",
          },
        },
    );
  }
}

const server = serve({
  routes: {
    "/api/*": proxy,
    "/categories": proxy,
    "/categories/*": proxy,
    "/*": index,
  },

  development: {
    hmr: true,
    console: true,
  },
});

console.log(`🚀 Satin Road running at ${server.url}`);
console.log(`🔌 API proxy: ${API_SERVER}`);
