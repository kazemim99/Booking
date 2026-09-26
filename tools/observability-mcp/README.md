# AsanRezerve observability MCP server

Lets an AI assistant (Claude Code, Claude Desktop, any MCP client) investigate the running system through the
admin observability API: logs, a whole request by trace id, the AI digest, the system overview, log levels and the
cache. Part of `openspec/changes/add-observability-and-caching` — see `docs/OBSERVABILITY.md`.

Logs leave the server only when your assistant asks for them, with your admin token. Messages are already masked by
the API (phone numbers, e-mails, OTP codes, passwords, tokens).

## Tools

| Tool | What it returns |
|---|---|
| `get_digest` | **Start here.** Markdown summary of a window (default: last hour): levels, grouped errors with sample trace ids, slowest routes, cache hit ratios. `source` narrows to a namespace. |
| `search_logs` | Stored events (14 days), filtered by window, level, text, source, trace id, path, status. |
| `get_log_event` | One event with template, exception and all properties. |
| `get_trace` | Every event of one request (the `X-Trace-Id` a client got, or the `traceId` in an error body). |
| `get_overview` | Health now: process, last-hour latency/errors, 24 h timeline, cache, log store, counters. |
| `list_log_levels` | Configured and effective level per category, with overrides. |
| `get_cache_stats` | Hit ratios per query, in-process tier, Redis circuit. |
| `set_log_level`, `reset_log_level`, `invalidate_cache` | Only with `ASANREZERVE_MCP_ALLOW_WRITES=true`. Audited under your admin account. |

## Setup

```bash
cd tools/observability-mcp && npm ci
```

Environment:

| Variable | Example |
|---|---|
| `ASANREZERVE_API_URL` | `https://back.nahalkmi.ir/api/v1` (or `http://localhost:5000/api/v1`) |
| `ASANREZERVE_ADMIN_TOKEN` | An admin JWT: sign in to the admin panel, then copy `admin_token` from the browser's local storage. It expires with the session (60 minutes); the server tells you when to renew it. |
| `ASANREZERVE_MCP_ALLOW_WRITES` | `true` to allow changing log levels and purging the cache. Leave unset for read-only. |

Claude Code (`claude mcp add`):

```bash
claude mcp add asanrezerve-observability \
  -e ASANREZERVE_API_URL=https://back.nahalkmi.ir/api/v1 \
  -e ASANREZERVE_ADMIN_TOKEN=<admin jwt> \
  -- node /absolute/path/to/Booking/tools/observability-mcp/src/server.mjs
```

Claude Desktop / any client: see `mcp.example.json`.

Try: *"Use get_digest for the last 6 hours and explain the top three errors; open a sample trace for each."*

## Tests

```bash
npm test
```
