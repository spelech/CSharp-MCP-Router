const fs = require('fs');

let modal = fs.readFileSync('frontend/src/components/servers/ServerModal.tsx', 'utf8');

modal = modal.replace(
  'const [hidden, setHidden] = useState(editingServer ? editingServer.hidden : false);',
  'const [hidden, setHidden] = useState(editingServer ? editingServer.hidden : false);\n  const [allowPassThroughAuth, setAllowPassThroughAuth] = useState(editingServer ? editingServer.allowPassThroughAuth : false);\n  const [dynamicAuthPrompt, setDynamicAuthPrompt] = useState(editingServer?.dynamicAuthPrompt || "");'
);

modal = modal.replace(
  '      enabled,\n      hidden,\n    };',
  '      enabled,\n      hidden,\n      allowPassThroughAuth,\n      dynamicAuthPrompt,\n    };'
);

modal = modal.replace(
  '<div className="modal-footer">',
  '          <div className="form-group">\n            <div className="checkbox-group" style={{ marginBottom: "10px" }}>\n              <label className="switch">\n                <input\n                  type="checkbox"\n                  checked={allowPassThroughAuth}\n                  onChange={(e) => setAllowPassThroughAuth(e.target.checked)}\n                />\n                <span className="slider"></span>\n              </label>\n              <span className="checkbox-label">Allow Dynamic Pass-Through Auth</span>\n            </div>\n            {allowPassThroughAuth && (\n              <div>\n                <label>Dynamic Auth Prompt Instructions</label>\n                <input\n                  type="text"\n                  placeholder="e.g. Provide a JWT token in target_auth_token parameter"\n                  value={dynamicAuthPrompt}\n                  onChange={(e) => setDynamicAuthPrompt(e.target.value)}\n                />\n              </div>\n            )}\n          </div>\n\n          <div className="modal-footer">'
);

fs.writeFileSync('frontend/src/components/servers/ServerModal.tsx', modal);

function fixTests() {
  const tests = [
    'frontend/src/test/components/DashboardView.test.tsx',
    'frontend/src/test/components/ServerCard.test.tsx',
    'frontend/src/test/components/ServerInspectModal.test.tsx',
    'frontend/src/test/components/ServerModal.test.tsx',
    'frontend/src/test/stores/useServerStore.test.ts'
  ];
  
  tests.forEach(f => {
    let t = fs.readFileSync(f, 'utf8');
    t = t.replace(/connectionError: '',/g, "connectionError: '',\n    allowPassThroughAuth: false,");
    t = t.replace(/connectionError: 'Server Offline',/g, "connectionError: 'Server Offline',\n    allowPassThroughAuth: false,");
    fs.writeFileSync(f, t);
  });
}
fixTests();

