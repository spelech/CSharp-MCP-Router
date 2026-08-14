const http = require('http');

const payload = {
  displayName: "Test Server",
  type: "stdio",
  categories: ["test"],
  url: "node /app/mock_stdio.js",
  secretProvider: "Environment",
  secretItemKey: "MY_APP_KEY",
  authShape: "bearer",
  enabled: true,
  hidden: false
};

const req = http.request('http://localhost:8088/api/servers', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Remote-User': 'admin',
    'Remote-Groups': 'full_admin',
    'Remote-User-Sid': 'S-1-5-32-544'
  }
}, (res) => {
  console.log('Status:', res.statusCode);
  res.on('data', d => process.stdout.write(d));
});

req.on('error', e => console.error(e));
req.write(JSON.stringify(payload));
req.end();
