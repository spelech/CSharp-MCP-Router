#!/usr/bin/env bash
set -e

# Change to the script's directory (repo root)
cd "$(dirname "$0")"

if [ -z "$1" ]; then
    echo "Usage: ./commit.sh \"<commit_message>\""
    exit 1
fi

echo "🔍 Validating project build..."
dotnet build ModelContextGateway.slnx --configuration Release

echo "🔄 Running automated version bump..."
python3 scripts/bump_version.py "$1"

echo "💾 Creating atomic commit: '$1'..."
git add -u
git commit -m "$1"
echo "✅ Version bumped and commit created successfully."
