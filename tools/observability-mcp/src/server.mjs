#!/usr/bin/env node
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js'
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js'
import { createAdminClient } from './client.mjs'
import { defineTools } from './tools.mjs'

/**
 * AsanRezerve observability over MCP (stdio). Configure with:
 *   ASANREZERVE_API_URL           e.g. https://back.nahalkmi.ir/api/v1
 *   ASANREZERVE_ADMIN_TOKEN       an admin JWT (sign in to the admin panel)
 *   ASANREZERVE_MCP_ALLOW_WRITES  "true" to also offer set_log_level, reset_log_level, invalidate_cache
 */
const client = createAdminClient({
  baseUrl: process.env.ASANREZERVE_API_URL,
  token: process.env.ASANREZERVE_ADMIN_TOKEN,
})
const allowWrites = process.env.ASANREZERVE_MCP_ALLOW_WRITES === 'true'

const server = new McpServer({ name: 'asanrezerve-observability', version: '1.0.0' })

for (const tool of defineTools(client, { allowWrites })) {
  server.registerTool(
    tool.name,
    {
      description: tool.description,
      inputSchema: tool.inputSchema,
      annotations: { readOnlyHint: tool.readOnly, openWorldHint: false },
    },
    async (args) => {
      try {
        return await tool.handler(args ?? {})
      } catch (error) {
        return { isError: true, content: [{ type: 'text', text: error instanceof Error ? error.message : String(error) }] }
      }
    },
  )
}

await server.connect(new StdioServerTransport())
