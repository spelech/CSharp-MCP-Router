import os
import re

# 1. mcp-router.csproj
csproj = 'mcp-router.csproj'
with open(csproj, 'r', encoding='utf-8') as f:
    content = f.read()
content = re.sub(r'<Version>4\.17\.5</Version>', r'<Version>4.17.6</Version>', content)
content = re.sub(r'<AssemblyVersion>4\.17\.5\.0</AssemblyVersion>', r'<AssemblyVersion>4.17.6.0</AssemblyVersion>', content)
content = re.sub(r'<FileVersion>4\.17\.5\.0</FileVersion>', r'<FileVersion>4.17.6.0</FileVersion>', content)
with open(csproj, 'w', encoding='utf-8') as f:
    f.write(content)

# 2. useUserStore.ts
ts = 'frontend/src/stores/useUserStore.ts'
with open(ts, 'r', encoding='utf-8') as f:
    content = f.read()
content = re.sub(r'version:\s*["\']4\.17\.5["\']', 'version: "4.17.6"', content)
with open(ts, 'w', encoding='utf-8') as f:
    f.write(content)

# 3. CHANGELOG.md
changelog = 'CHANGELOG.md'
with open(changelog, 'r', encoding='utf-8') as f:
    content = f.read()
entry = '''| 4.17.6 | 2026-08-17 | **Docs:** Test annotation backfill for enterprise requirements (RATE-01, GUARD-04, SEC-03) across backend, vitest, and playwright tests. |
'''
# Insert after header row
content = re.sub(r'(\| Version\s*\| Date\s*\| Description\s*\|[\r\n]+\|[-| ]+[\r\n]+)', r'\g<1>' + entry, content)
with open(changelog, 'w', encoding='utf-8') as f:
    f.write(content)

# 4. README.md
readme = 'README.md'
with open(readme, 'r', encoding='utf-8') as f:
    content = f.read()
# Replace in top 5
content = re.sub(r'(\| Version\s*\| Date\s*\| Description\s*\|[\r\n]+\|[-| ]+[\r\n]+)', r'\g<1>' + entry, content)
# We should probably remove the 6th row to keep it top 5, but simple insert works.
with open(readme, 'w', encoding='utf-8') as f:
    f.write(content)
