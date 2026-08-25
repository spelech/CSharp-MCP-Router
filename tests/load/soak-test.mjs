import http from 'http';

const MCG_URL = process.env.MCG_URL || 'http://localhost:8080';
const CONCURRENT_CLIENTS = 20;
const SOAK_DURATION_MS = 30000; // 30 seconds

async function getDiagnostics() {
    const res = await fetch(`${MCG_URL}/api/diagnostics`, {
        headers: {
            'Remote-User': 'admin',
            'Remote-Groups': 'full_admin'
        }
    });
    if (!res.ok) throw new Error(`Failed to get diagnostics: ${res.status}`);
    return await res.json();
}

async function runClient(durationMs) {
    const startTime = Date.now();
    while (Date.now() - startTime < durationMs) {
        let abortController = new AbortController();
        try {
            // Establish SSE connection
            const req = http.request(`${MCG_URL}/sse`, {
                method: 'GET',
                headers: { 
                    'Accept': 'text/event-stream',
                    'Remote-User': 'admin',
                    'Remote-Groups': 'full_admin'
                }
            });
            
            let sessionId = null;
            
            req.on('response', (res) => {
                res.on('data', (chunk) => {
                    const data = chunk.toString();
                    if (data.includes('endpoint')) {
                        try {
                            const match = data.match(/data:\s*({.*})/);
                            if (match && match[1]) {
                                const parsed = JSON.parse(match[1]);
                                if (parsed.endpoint) {
                                    const url = new URL(parsed.endpoint, MCG_URL);
                                    sessionId = url.searchParams.get('sessionId');
                                }
                            }
                        } catch (e) {
                            // ignore parse errors
                        }
                    }
                });
            });
            
            req.on('error', () => {});
            req.end();
            
            // Wait for sessionId
            let waitStart = Date.now();
            while (!sessionId && Date.now() - waitStart < 2000) {
                await new Promise(r => setTimeout(r, 50));
            }
            
            if (sessionId) {
                // Send a request
                try {
                    await fetch(`${MCG_URL}/message?sessionId=${sessionId}`, {
                        method: 'POST',
                        headers: { 
                            'Content-Type': 'application/json',
                            'Remote-User': 'admin',
                            'Remote-Groups': 'full_admin'
                        },
                        body: JSON.stringify({
                            jsonrpc: '2.0',
                            id: 1,
                            method: 'tools/list'
                        }),
                        signal: abortController.signal
                    }).catch(() => {});
                } catch (e) {}
            }
            
            // Abruptly destroy the socket to test cleanup
            req.destroy();
            abortController.abort();
            
        } catch (e) {
            // ignore network errors
        }
        await new Promise(r => setTimeout(r, Math.random() * 200 + 100)); // sleep 100-300ms
    }
}

async function main() {
    console.log(`Starting soak test against ${MCG_URL} for ${SOAK_DURATION_MS}ms with ${CONCURRENT_CLIENTS} clients...`);
    
    let initialDiag;
    try {
        initialDiag = await getDiagnostics();
        console.log('Initial diagnostics:', initialDiag);
    } catch (e) {
        console.error('Could not reach router. Error:', e.message);
        process.exit(1);
    }
    
    const workers = [];
    for (let i = 0; i < CONCURRENT_CLIENTS; i++) {
        workers.push(runClient(SOAK_DURATION_MS));
    }
    
    await Promise.all(workers);
    console.log('Load test complete. Waiting 3 seconds for cleanup sweeps...');
    await new Promise(r => setTimeout(r, 3000));
    
    const finalDiag = await getDiagnostics();
    console.log('Final diagnostics:', finalDiag);
    
    let failed = false;
    
    if (finalDiag.activeSessions > initialDiag.activeSessions + 5) {
        console.error(`❌ Session leak detected: ${initialDiag.activeSessions} -> ${finalDiag.activeSessions}`);
        failed = true;
    } else {
        console.log('✅ Active sessions bounded.');
    }
    
    if (finalDiag.pendingApprovals > initialDiag.pendingApprovals + 5) {
        console.error(`❌ Pending approvals leak detected: ${initialDiag.pendingApprovals} -> ${finalDiag.pendingApprovals}`);
        failed = true;
    } else {
        console.log('✅ Pending approvals bounded.');
    }
    
    const handleDelta = finalDiag.handleCount - initialDiag.handleCount;
    if (handleDelta > 30) {
        console.error(`❌ Handle/Socket leak detected: ${initialDiag.handleCount} -> ${finalDiag.handleCount} (Delta: ${handleDelta})`);
        failed = true;
    } else {
        console.log(`✅ Handles bounded (Delta: ${handleDelta}).`);
    }
    
    if (failed) {
        console.error('Soak test FAILED.');
        process.exit(1);
    } else {
        console.log('Soak test PASSED.');
    }
}

main();
