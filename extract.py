import os

source_file = '/containers/dev/csharp-mcp-router/Core/ClientSession.cs'
dest_dir = '/containers/dev/csharp-mcp-router/Core/ClientSession'
dest_file = os.path.join(dest_dir, 'ClientSession.Authorization.cs')

if not os.path.exists(dest_dir):
    os.makedirs(dest_dir)

with open(source_file, 'r') as f:
    lines = f.readlines()

new_lines = []
extracted_lines = []

def extract_range(start_line, end_line):
    # lines are 1-indexed for our reference
    return lines[start_line-1:end_line]

chunk1 = extract_range(68, 263)
chunk2 = extract_range(837, 853)
chunk3 = extract_range(903, 987)

# We want to keep all lines EXCEPT the ones we extract
for i, line in enumerate(lines):
    line_num = i + 1
    if (68 <= line_num <= 266) or (837 <= line_num <= 854) or (903 <= line_num <= 988):
        continue
    new_lines.append(line)

with open(source_file, 'w') as f:
    f.writelines(new_lines)

auth_class = [
    "using System;\n",
    "using System.Collections.Generic;\n",
    "using System.Linq;\n",
    "using System.Text.Json;\n",
    "using System.Threading.Tasks;\n",
    "using Microsoft.AspNetCore.Http;\n",
    "using Microsoft.Extensions.DependencyInjection;\n",
    "using Microsoft.Extensions.Logging;\n",
    "using Microsoft.Extensions.Configuration;\n",
    "using Dapper;\n",
    "using McpRouter.Core.Identity;\n",
    "using McpRouter.Core.Database;\n",
    "\n",
    "namespace McpRouter\n",
    "{\n",
    "    public partial class ClientSession\n",
    "    {\n"
]

auth_class.extend(chunk1)
auth_class.append("\n")
auth_class.extend(chunk2)
auth_class.append("\n")
auth_class.extend(chunk3)
auth_class.append("    }\n")
auth_class.append("}\n")

with open(dest_file, 'w') as f:
    f.writelines(auth_class)

print("Extraction complete")
