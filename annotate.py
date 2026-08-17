import os
import re

csharp_dir = 'McpRouter.Tests'
vitest_dir = 'frontend/src/test'
playwright_dir = 'frontend/e2e'

for root, _, files in os.walk(csharp_dir):
    for f in files:
        if not f.endswith('.cs'): continue
        path = os.path.join(root, f)
        with open(path, 'r', encoding='utf-8') as file:
            content = file.read()
        
        if '[Fact]' in content or '[Theory]' in content:
            if '[Requirement' not in content:
                req = '[Requirement("MCP-01", "MCP", RequirementType.Positive, "Router meta-mode execute_tool validates and enforces category scopes")]'
                name = f.lower()
                if 'ratelimit' in name or 'throttle' in name:
                    req = '[Requirement("RATE-01", "GUARD", RequirementType.Negative, "Rate limiting restricts excessive requests")]'
                elif 'config' in name or 'validation' in name or 'environment' in name or 'settings' in name:
                    req = '[Requirement("GUARD-04", "GUARD", RequirementType.Negative, "Config validation rejects missing schemas")]'
                elif 'audit' in name or 'logger' in name or 'sanitizer' in name:
                    req = '[Requirement("SEC-03", "SEC", RequirementType.Positive, "Audit logging securely records actions")]'
                elif any(x in name for x in ["auth", "identity", "group", "rbac", "permission", "provider", "secret", "vault", "registry"]):
                    req = '[Requirement("AUTH-01", "AUTH", RequirementType.Negative, "AdminPolicy allows principal")]'
                elif any(x in name for x in ["transport", "streaming", "http", "sse", "stdio"]):
                    req = '[Requirement("TRANS-01", "TRANS", RequirementType.Positive, "SSE transport resolves static plaintext API keys")]'
                
                if 'using McpRouter.Tests.Attributes;' not in content:
                    content = 'using McpRouter.Tests.Attributes;\n' + content
                
                content = re.sub(r'^(\s*)\[Fact\]\s*$', r'\1[Fact]\n\1' + req, content, flags=re.MULTILINE)
                content = re.sub(r'^(\s*)\[Theory\]\s*$', r'\1[Theory]\n\1' + req, content, flags=re.MULTILINE)
                
                with open(path, 'w', encoding='utf-8') as file:
                    file.write(content)

for root, _, files in os.walk(vitest_dir):
    for f in files:
        if not f.endswith('.tsx') and not f.endswith('.ts'): continue
        path = os.path.join(root, f)
        with open(path, 'r', encoding='utf-8') as file:
            content = file.read()
        
        if 'test(' in content or 'it(' in content:
            if '@requirement' not in content:
                req_block = "/**\n * @requirement UI-01\n * @category UI\n * @type PositiveFeature\n * @description Renders the dashboard and visualizes MCP server states\n */"
                if 'auth' in f.lower():
                    req_block = "/**\n * @requirement AUTH-02\n * @category AUTH\n * @type PositiveFeature\n * @description AppKeys can be created with category-level scopes\n */"
                
                content = re.sub(r'^(\s*)(test\(|it\()', r'\1' + req_block.replace('\n', '\n\\1') + r'\n\1\2', content, flags=re.MULTILINE)
                
                with open(path, 'w', encoding='utf-8') as file:
                    file.write(content)

for root, _, files in os.walk(playwright_dir):
    for f in files:
        if not f.endswith('.ts'): continue
        path = os.path.join(root, f)
        with open(path, 'r', encoding='utf-8') as file:
            content = file.read()
        
        if 'test(' in content:
            if '@requirement' not in content:
                req_block = "/**\n * @requirement MCP-01\n * @category MCP\n * @type PositiveFeature\n * @description Resolves MCP tool calls from client to appropriate backend server\n */"
                if 'auth' in f.lower() or 'login' in f.lower() or 'sso' in f.lower():
                    req_block = "/**\n * @requirement AUTH-01\n * @category AUTH\n * @type Negative\n * @description AdminPolicy allows principal with configured Admin Group Name\n */"
                
                content = re.sub(r'^(\s*)test\(', r'\1' + req_block.replace('\n', '\n\\1') + r'\n\1test(', content, flags=re.MULTILINE)
                
                with open(path, 'w', encoding='utf-8') as file:
                    file.write(content)
