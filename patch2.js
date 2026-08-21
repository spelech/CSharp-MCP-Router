const fs = require('fs');

const f1 = 'frontend/src/test/components/ServerModal.test.tsx';
let t1 = fs.readFileSync(f1, 'utf8');
t1 = t1.replace(/connectionError: 'Test error'/g, "connectionError: 'Test error',\n    allowPassThroughAuth: false");
fs.writeFileSync(f1, t1);

const f2 = 'frontend/src/test/stores/useServerStore.test.ts';
let t2 = fs.readFileSync(f2, 'utf8');
t2 = t2.replace(/connectionError: '',/g, "connectionError: '',\n    allowPassThroughAuth: false,");
fs.writeFileSync(f2, t2);

