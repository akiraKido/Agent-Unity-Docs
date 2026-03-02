#!/usr/bin/env bash
set -euo pipefail

# ============================================================
# Unity Docs Skill - Setup Script
# Downloads Unity documentation and generates a compact index.
# Usage: bash setup.sh [VERSION]
#   VERSION: Optional Unity version (e.g., 6000.0). If omitted,
#            auto-detects from ProjectSettings/ProjectVersion.txt.
# ============================================================

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SKILL_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
PROJECT_ROOT="$(git rev-parse --show-toplevel 2>/dev/null)" || PROJECT_ROOT="$(cd "$SKILL_DIR/../../.." && pwd)"

DOCS_DIR="$SKILL_DIR/docs"
INDEX_DIR="$SKILL_DIR/references"
INDEX_FILE="$INDEX_DIR/index.txt"
CDN_CONFIG="$SKILL_DIR/cdn_versions.json"

# ---- Version Detection ----
VERSION="${1:-}"

if [[ -z "$VERSION" ]]; then
    VERSION_FILE="$PROJECT_ROOT/ProjectSettings/ProjectVersion.txt"
    if [[ ! -f "$VERSION_FILE" ]]; then
        echo "Error: ProjectSettings/ProjectVersion.txt not found at $PROJECT_ROOT"
        echo "→ Specify version manually: bash setup.sh 6000.0"
        exit 1
    fi
    FULL_VERSION=$(grep 'm_EditorVersion:' "$VERSION_FILE" | head -1 | awk '{print $2}')
    VERSION=$(echo "$FULL_VERSION" | grep -oE '^[0-9]+\.[0-9]+')
    echo "Auto-detected Unity version: $FULL_VERSION (normalized: $VERSION)"
else
    echo "Using specified version: $VERSION"
fi

if [[ -z "$VERSION" ]]; then
    echo "Error: Could not determine Unity version"
    echo "→ Specify version manually: bash setup.sh 6000.0"
    exit 1
fi

# ---- Skip if already downloaded ----
SKIP_DOWNLOAD=false

if [[ -d "$DOCS_DIR/Manual" ]]; then
    echo "Documentation already exists at $DOCS_DIR/Manual"
    echo "Regenerating index only. To re-download, delete $DOCS_DIR first."
    SKIP_DOWNLOAD=true
fi

# ---- Migrate from legacy .unity-docs/ ----
LEGACY_DOCS="$PROJECT_ROOT/.unity-docs"

if [[ "$SKIP_DOWNLOAD" == "false" && -d "$LEGACY_DOCS/Manual" ]]; then
    echo "Found legacy .unity-docs/ at $LEGACY_DOCS"
    echo "Moving to skill directory..."
    mkdir -p "$DOCS_DIR"
    mv "$LEGACY_DOCS/"* "$DOCS_DIR/" 2>/dev/null || true
    rmdir "$LEGACY_DOCS" 2>/dev/null || true
    echo "Migrated .unity-docs/ → $DOCS_DIR"
    SKIP_DOWNLOAD=true
fi

# ---- CDN URL Resolution + Download ----
if [[ "$SKIP_DOWNLOAD" == "false" ]]; then
    CDN_URL=$(python3 -c "
import json, sys
with open('$CDN_CONFIG') as f:
    data = json.load(f)
version = '$VERSION'
if version in data.get('cloudmedia', []):
    base = data.get('cloudmedia_base', 'https://cloudmedia-docs.unity3d.com/docscloudstorage/en')
    print(f'{base}/{version}/UnityDocumentation.zip')
elif version in data.get('google_storage_only', []):
    base = data.get('google_storage_base', 'https://storage.googleapis.com/docscloudstorage')
    print(f'{base}/{version}/UnityDocumentation.zip')
else:
    # Default: try cloudmedia first
    base = data.get('cloudmedia_base', 'https://cloudmedia-docs.unity3d.com/docscloudstorage/en')
    print(f'{base}/{version}/UnityDocumentation.zip')
")

    echo "Download URL: $CDN_URL"

    # ---- Download ----
    mkdir -p "$DOCS_DIR"
    TEMP_ZIP="$DOCS_DIR/temp-docs.zip"

    echo "Downloading Unity $VERSION documentation..."
    if ! curl -fSL --progress-bar -o "$TEMP_ZIP" "$CDN_URL"; then
        echo ""
        echo "Error: Failed to download from $CDN_URL"
        echo "→ Check your internet connection"
        echo "→ Verify the version ($VERSION) is available"
        echo "→ Try specifying a different version: bash setup.sh <VERSION>"
        rm -f "$TEMP_ZIP"
        rmdir "$DOCS_DIR" 2>/dev/null || true
        exit 1
    fi

    # ---- Extract ----
    echo "Extracting documentation..."
    if ! unzip -qo "$TEMP_ZIP" -d "$DOCS_DIR"; then
        echo "Error: Failed to extract documentation"
        echo "→ Delete $DOCS_DIR and try again"
        rm -f "$TEMP_ZIP"
        exit 1
    fi
    rm -f "$TEMP_ZIP"

    # Handle nested structure: Documentation/en/ or en/
    if [[ -d "$DOCS_DIR/Documentation/en" ]]; then
        echo "Restructuring from Documentation/en/..."
        for item in "$DOCS_DIR/Documentation/en/"*; do
            mv -f "$item" "$DOCS_DIR/"
        done
        rm -rf "$DOCS_DIR/Documentation"
    elif [[ -d "$DOCS_DIR/en" ]]; then
        echo "Restructuring from en/..."
        for item in "$DOCS_DIR/en/"*; do
            mv -f "$item" "$DOCS_DIR/"
        done
        rm -rf "$DOCS_DIR/en"
    fi

    echo "Documentation extracted to $DOCS_DIR"
fi

# ---- Clean legacy index from CLAUDE.md ----
START_MARKER="<!-- UNITY-DOCS-INDEX-START -->"
END_MARKER="<!-- UNITY-DOCS-INDEX-END -->"

CLAUDE_MD="$PROJECT_ROOT/CLAUDE.md"

if [[ -f "$CLAUDE_MD" ]] && grep -q "$START_MARKER" "$CLAUDE_MD" 2>/dev/null; then
    echo "Removing legacy index from CLAUDE.md..."
    python3 -c "
filepath = '$CLAUDE_MD'
start = '$START_MARKER'
end = '$END_MARKER'
with open(filepath, 'r') as f:
    content = f.read()
si = content.find(start)
ei = content.find(end)
if si != -1 and ei != -1 and ei > si:
    before = content[:si].rstrip('\n')
    after = content[ei + len(end):].lstrip('\n')
    if before and after:
        content = before + '\n' + after
    elif before:
        content = before + '\n'
    else:
        content = after
    with open(filepath, 'w') as f:
        f.write(content)
    print('  Cleaned CLAUDE.md')
else:
    print('  Markers incomplete in CLAUDE.md, skipped')
"
fi

# ---- Generate Index ----
echo "Generating index..."
mkdir -p "$INDEX_DIR"

# Use python3 for reliable index generation
SKILL_DIR="$SKILL_DIR" VERSION="$VERSION" python3 << 'PYEOF'
import os, sys

skill_dir = os.environ.get("SKILL_DIR", ".")
docs_dir = os.path.join(skill_dir, "docs")
version = os.environ.get("VERSION", "")
index_file = os.path.join(skill_dir, "references", "index.txt")

manual_dir = os.path.join(docs_dir, "Manual")

if not os.path.isdir(manual_dir):
    print(f"Error: Manual directory not found at {manual_dir}")
    sys.exit(1)

# Collect all HTML files
files = []
for root, dirs, filenames in os.walk(manual_dir):
    for f in filenames:
        if f.endswith('.html'):
            rel = os.path.relpath(os.path.join(root, f), manual_dir)
            files.append(rel.replace('\\', '/'))

files.sort()

# Group by directory
groups = {}
for f in files:
    parts = f.rsplit('/', 1)
    if len(parts) == 1:
        dir_key = '.'
        fname = parts[0]
    else:
        dir_key = parts[0]
        fname = parts[1]
    groups.setdefault(dir_key, []).append(fname)

# Compute relative root from project root
# Skill is at .claude/skills/unity-docs/, docs at .claude/skills/unity-docs/docs/
root_path = ".claude/skills/unity-docs/docs"

# Build index
parts = ['[Unity Docs Index]']
parts.append(f'root: {root_path}')
parts.append(f'version: {version}')
parts.append('IMPORTANT: Prefer retrieval-led reasoning over pre-training-led reasoning for any Unity tasks.')
parts.append(f'ScriptReference (API docs) available at: {root_path}/ScriptReference/ (not indexed due to size)')

for dir_key in sorted(groups.keys()):
    prefix = 'Manual' if dir_key == '.' else f'Manual/{dir_key}'
    parts.append(f'{prefix}:{{{",".join(groups[dir_key])}}}')

with open(index_file, 'w') as f:
    f.write('|'.join(parts))

print(f"Index generated: {len(files)} files indexed → {index_file}")
PYEOF

echo ""
echo "Setup complete!"
echo "  Docs: $DOCS_DIR"
echo "  Index: $INDEX_FILE"
