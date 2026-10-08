import { resolve } from "node:path";

const clientDir = resolve(import.meta.dir, "..");
const apiDir = resolve(clientDir, "..", "server", "API");
const apiUrl = "http://127.0.0.1:5015";

console.log("🧵 Starting Satin Road...\n");

const backend = Bun.spawn(["dotnet", "run", "--project", "API.csproj", "--urls", apiUrl], {
  cwd: apiDir,
  stdin: "inherit",
  stdout: "inherit",
  stderr: "inherit",
});

async function waitForApi(timeoutMs = 30000) {
  const started = Date.now();
  while (Date.now() - started < timeoutMs) {
    try {
      const response = await fetch(`${apiUrl}/swagger/v1/swagger.json`);
      if (response.ok) return;
    } catch {
      // Keep waiting while .NET starts.
    }
    await Bun.sleep(250);
  }
  throw new Error("The .NET API did not become ready within 30 seconds.");
}

try {
  console.log("⏳ Starting .NET API...");
  await waitForApi();
  console.log(" API ready. Starting frontend...\n");
} catch (error) {
  console.error(` ${error instanceof Error ? error.message : error}`);
  backend.kill();
  process.exit(1);
}

const frontend = Bun.spawn(["bun", "--hot", "src/index.ts"], {
  cwd: clientDir,
  stdin: "inherit",
  stdout: "inherit",
  stderr: "inherit",
});

let shuttingDown = false;
async function shutdown(code = 0) {
  if (shuttingDown) return;
  shuttingDown = true;
  console.log("\n Stopping Satin Road...");
  frontend.kill();
  backend.kill();
  await Promise.allSettled([frontend.exited, backend.exited]);
  process.exit(code);
}

process.on("SIGINT", () => void shutdown(0));
process.on("SIGTERM", () => void shutdown(0));

const exitCode = await frontend.exited;
await shutdown(exitCode);
