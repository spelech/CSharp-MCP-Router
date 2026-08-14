const http = require('http');

const req = http.request('http://localhost:8088/api/servers', {
  method: 'GET',
  headers: {
    'Remote-User': 'admin',
    'Remote-Groups': 'full_admin',
    'Remote-User-Sid': 'S-1-5-32-544'
  }
}, (res) => {
  console.log('Status:', res.statusCode);
  res.on('data', d => process.stdout.write(d));
});

req.on('error', e => console.error(e));
req.end();
