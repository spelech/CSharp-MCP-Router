with open('Core/ClientSession.cs', 'r') as f:
    lines = f.readlines()
# The last line is "}\n" with maybe some blank lines.
# Find the last "}\n" and insert "    }\n" before it.
for i in range(len(lines)-1, -1, -1):
    if lines[i].strip() == '}':
        lines.insert(i, "    }\n")
        break
with open('Core/ClientSession.cs', 'w') as f:
    f.writelines(lines)
