const readline = require('readline');

const rl = readline.createInterface({
  input: process.stdin,
  output: process.stdout,
  terminal: false
});

// To test environment variable resolution:
if (process.env.TEST_API_KEY) {
  console.error(`INIT_WITH_ENV: ${process.env.TEST_API_KEY}`);
}

rl.on('line', (line) => {
  if (!line.trim()) return;
  try {
    const request = JSON.parse(line);
    const { method, id, params } = request;

    if (method === 'initialize') {
      const response = {
        jsonrpc: '2.0',
        id: id,
        result: {
          protocolVersion: '2024-11-05',
          capabilities: {
            tools: {}
          },
          serverInfo: {
            name: 'MockStdioServer',
            version: '1.0.0'
          }
        }
      };
      console.log(JSON.stringify(response));
    } else if (method === 'notifications/initialized') {
      console.error("Initialized notification received.");
    } else if (method === 'tools/list') {
      const response = {
        jsonrpc: '2.0',
        id: id,
        result: {
          tools: [
            {
              name: 'echo',
              description: 'Echoes the input string',
              inputSchema: {
                type: 'object',
                properties: {
                  message: { type: 'string' }
                },
                required: ['message']
              }
            },
            {
              name: 'error_tool',
              description: 'Throws an error',
              inputSchema: { type: 'object' }
            },
            {
              name: 'slow_tool',
              description: 'Sleeps before responding',
              inputSchema: { type: 'object' }
            },
            {
              name: 'stderr_tool',
              description: 'Logs to stderr then returns',
              inputSchema: { type: 'object' }
            }
          ]
        }
      };
      console.log(JSON.stringify(response));
    } else if (method === 'tools/call') {
      const toolName = params.name;
      const args = params.arguments || {};

      if (toolName === 'echo') {
        const response = {
          jsonrpc: '2.0',
          id: id,
          result: {
            content: [
              {
                type: 'text',
                text: args.message || ''
              }
            ]
          }
        };
        console.log(JSON.stringify(response));
      } else if (toolName === 'error_tool') {
        const response = {
          jsonrpc: '2.0',
          id: id,
          error: {
            code: -32603,
            message: 'Internal error in mock tool execution'
          }
        };
        console.log(JSON.stringify(response));
      } else if (toolName === 'slow_tool') {
        // Sleep for 20 seconds to test timeout
        setTimeout(() => {
          const response = {
            jsonrpc: '2.0',
            id: id,
            result: {
              content: [{ type: 'text', text: 'too late' }]
            }
          };
          console.log(JSON.stringify(response));
        }, 20000);
      } else if (toolName === 'stderr_tool') {
        console.error("LOG_FROM_STDERR_TOOL");
        const response = {
          jsonrpc: '2.0',
          id: id,
          result: {
            content: [{ type: 'text', text: 'logged' }]
          }
        };
        console.log(JSON.stringify(response));
      } else {
        const response = {
          jsonrpc: '2.0',
          id: id,
          error: {
            code: -32601,
            message: `Method not found: ${toolName}`
          }
        };
        console.log(JSON.stringify(response));
      }
    } else {
      if (id !== undefined) {
        const response = {
          jsonrpc: '2.0',
          id: id,
          error: {
            code: -32601,
            message: `Method not found: ${method}`
          }
        };
        console.log(JSON.stringify(response));
      }
    }
  } catch (err) {
    console.error(`Parse error: ${err.message}`);
  }
});
