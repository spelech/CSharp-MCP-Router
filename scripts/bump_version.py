#!/usr/bin/env python3
import sys
import re
import os
from datetime import datetime

def main():
    if len(sys.argv) < 2:
        print("Usage: python3 bump_version.py <commit_message>")
        sys.exit(1)

    commit_message = sys.argv[1].strip()
    
    # Paths relative to repository root
    repo_root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    csproj_path = os.path.join(repo_root, "mcp-router.csproj")
    html_path = os.path.join(repo_root, "wwwroot", "index.html")
    readme_path = os.path.join(repo_root, "README.md")

    # Read current version from csproj
    if not os.path.exists(csproj_path):
        print(f"Error: {csproj_path} not found.")
        sys.exit(1)

    with open(csproj_path, "r", encoding="utf-8") as f:
        csproj_content = f.read()

    version_match = re.search(r"<Version>(.*?)</Version>", csproj_content)
    if not version_match:
        print("Error: Could not find <Version> in csproj.")
        sys.exit(1)

    current_version = version_match.group(1)
    print(f"Current version: {current_version}")

    # Parse version parts
    try:
        parts = list(map(int, current_version.split(".")))
        if len(parts) != 3:
            raise ValueError
    except ValueError:
        print(f"Error: Version format in csproj '{current_version}' is not X.Y.Z.")
        sys.exit(1)

    major, minor, patch = parts

    # Determine bump type based on Conventional Commits convention
    is_minor = False
    # Check for feat or breaking change features
    if commit_message.startswith("feat:") or commit_message.startswith("feat(") or "breaking change" in commit_message.lower():
        is_minor = True

    if is_minor:
        minor += 1
        patch = 0
    else:
        patch += 1

    new_version = f"{major}.{minor}.{patch}"
    print(f"New version computed: {new_version} (Bump type: {'Minor' if is_minor else 'Patch'})")

    # 1. Update csproj content
    updated_csproj = re.sub(r"<Version>.*?</Version>", f"<Version>{new_version}</Version>", csproj_content)
    updated_csproj = re.sub(r"<AssemblyVersion>.*?</AssemblyVersion>", f"<AssemblyVersion>{new_version}.0</AssemblyVersion>", updated_csproj)
    updated_csproj = re.sub(r"<FileVersion>.*?</FileVersion>", f"<FileVersion>{new_version}.0</FileVersion>", updated_csproj)

    with open(csproj_path, "w", encoding="utf-8", newline="\n") as f:
        f.write(updated_csproj)
    print(f"Updated {csproj_path}")

    # 2. Update index.html
    if os.path.exists(html_path):
        with open(html_path, "r", encoding="utf-8") as f:
            html_content = f.read()
        
        # Replace version badge: <span class="badge badge-primary" id="version-badge">vX.Y.Z</span>
        updated_html = re.sub(
            r'(<span[^>]*id="version-badge"[^>]*>v).*?(</span>)',
            fr'\g<1>{new_version}\g<2>',
            html_content
        )
        
        with open(html_path, "w", encoding="utf-8", newline="\n") as f:
            f.write(updated_html)
        print(f"Updated {html_path}")
    else:
        print(f"Warning: {html_path} not found.")

    # 3. Update README.md Release Changelog
    if os.path.exists(readme_path):
        with open(readme_path, "r", encoding="utf-8") as f:
            readme_content = f.read()

        today_str = datetime.now().strftime("%Y-%m-%d")
        # Strip conventional prefix if present for a cleaner changelog, or keep full message
        clean_msg = commit_message
        new_row = f"| **`v{new_version}`** | {today_str} | {clean_msg} |"

        # Locate the table header separator row: | :--- | :--- | :--- |
        # We want to insert the new row immediately after this separator.
        lines = readme_content.splitlines()
        inserted = False
        for i, line in enumerate(lines):
            if re.match(r"\|\s*:?---:?\s*\|\s*:?---:?\s*\|\s*:?---:?\s*\|", line):
                lines.insert(i + 1, new_row)
                inserted = True
                break
        
        if inserted:
            with open(readme_path, "w", encoding="utf-8", newline="\n") as f:
                f.write("\n".join(lines) + "\n")
            print(f"Updated {readme_path}")
        else:
            print("Warning: Could not find changelog table header separator in README.md.")
    else:
        print(f"Warning: {readme_path} not found.")

    # 4. Update useUserStore.ts
    user_store_path = os.path.join(repo_root, "frontend", "src", "stores", "useUserStore.ts")
    if os.path.exists(user_store_path):
        with open(user_store_path, "r", encoding="utf-8") as f:
            store_content = f.read()
        
        # Replace: version: 'X.Y.Z', // fallback default
        updated_store = re.sub(
            r"(version:\s*['\"]).*?('\s*,\s*//\s*fallback\s*default)",
            fr"\g<1>{new_version}\g<2>",
            store_content
        )
        
        with open(user_store_path, "w", encoding="utf-8", newline="\n") as f:
            f.write(updated_store)
        print(f"Updated {user_store_path}")
    else:
        print(f"Warning: {user_store_path} not found.")

    # 5. Stage files using git add
    paths_to_stage = [csproj_path, html_path, readme_path, user_store_path]
    existing_paths = [p for p in paths_to_stage if os.path.exists(p)]
    os.system(f"git add {' '.join(existing_paths)}")
    print("Staged updated versioning files.")

if __name__ == "__main__":
    main()
