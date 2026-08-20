import re

files = [
    'Infrastructure/Transports/HttpTransport.cs',
    'Infrastructure/Transports/SseTransport.cs',
    'Infrastructure/Transports/StdioTransport.cs'
]

pattern = re.compile(r'if \(_server\.AllowPassThroughAuth && !string\.IsNullOrEmpty\(_passThroughToken\)\)\s*\{\s*return _passThroughToken;\s*\}')
replacement = """if (!string.IsNullOrEmpty(_passThroughToken) && (_server.AllowPassThroughAuth || _server.SecretProvider == "UserProvided"))
            {
                return _passThroughToken;
            }"""

for path in files:
    with open(path, 'r', newline='') as f:
        content = f.read()
    
    new_content, count = pattern.subn(replacement, content)
    if count > 0:
        with open(path, 'w', newline='') as f:
            f.write(new_content)
        print(f"Updated {path}")
    else:
        print(f"Target not found in {path}")
