#!/bin/bash
# Updates the cdn_versions.json with versions available on cloudmedia CDN
# Uses agent-browser to get the latest version list from Unity docs

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
JSON_FILE="$SCRIPT_DIR/../Editor/cdn_versions.json"

# Check if agent-browser is installed
if ! command -v agent-browser &> /dev/null; then
  echo "Error: agent-browser is not installed"
  echo "Install with: npm install -g agent-browser"
  exit 1
fi

echo "Fetching version list from Unity docs..."

# Open Unity docs page and get version list via agent-browser
agent-browser open "https://docs.unity3d.com/Manual/OfflineDocumentation.html"
sleep 3

# Accept cookies if present
agent-browser eval 'document.querySelector("button[id*=\"accept\"], button[class*=\"accept\"]")?.click()' > /dev/null 2>&1 || true
sleep 1

# Get all versions from the version selector dropdown (comma-separated)
RAW_VERSIONS=$(agent-browser eval '(() => {
  const select = document.querySelector("select[id*=\"version\"]");
  if (!select) return "";
  const versions = [];
  for (const opt of select.options) {
    const match = opt.id?.match(/^(\d+)-(\d+)$/);
    if (match) {
      versions.push(match[1] + "." + match[2]);
    }
  }
  return versions.join(",");
})()' 2>/dev/null | tr -d '"')

# Convert comma-separated to newline-separated and filter valid versions
VERSIONS=$(echo "$RAW_VERSIONS" | tr ',' '\n' | grep -E '^[0-9]+\.[0-9]+$' | sort -u -V -r)

if [ -z "$VERSIONS" ]; then
  echo "Failed to get versions from Unity docs, using fallback list"
  VERSIONS=$(cat << 'EOF'
6000.5
6000.4
6000.3
6000.2
6000.1
6000.0
2023.2
2023.1
2022.3
2022.2
2022.1
2021.3
2021.2
2021.1
2020.3
2020.2
2020.1
2019.4
2019.3
2019.2
2019.1
2018.4
2018.3
2018.2
2018.1
2017.4
2017.3
2017.2
2017.1
EOF
)
fi

echo "Found versions:"
echo "$VERSIONS" | head -5
echo "..."

CLOUDMEDIA_VERSIONS=()

echo ""
echo "Checking cloudmedia availability..."

for VERSION in $VERSIONS; do
  URL="https://cloudmedia-docs.unity3d.com/docscloudstorage/en/${VERSION}/UnityDocumentation.zip"
  STATUS=$(curl -sI -o /dev/null -w "%{http_code}" "$URL" --connect-timeout 5)

  if [ "$STATUS" = "200" ]; then
    echo "  $VERSION: cloudmedia OK"
    CLOUDMEDIA_VERSIONS+=("$VERSION")
  else
    echo "  $VERSION: Google Storage (cloudmedia: $STATUS)"
  fi
done

# Generate JSON
echo ""
echo "Generating $JSON_FILE..."

{
  echo "{"
  echo "  \"cloudmedia\": ["
  count=${#CLOUDMEDIA_VERSIONS[@]}
  i=0
  for v in "${CLOUDMEDIA_VERSIONS[@]}"; do
    i=$((i + 1))
    if [ $i -eq $count ]; then
      echo "    \"$v\""
    else
      echo "    \"$v\","
    fi
  done
  echo "  ],"
  echo "  \"google_storage_base\": \"https://storage.googleapis.com/docscloudstorage\","
  echo "  \"cloudmedia_base\": \"https://cloudmedia-docs.unity3d.com/docscloudstorage/en\""
  echo "}"
} > "$JSON_FILE"

echo ""
echo "Done! Cloudmedia versions: ${#CLOUDMEDIA_VERSIONS[@]}"
cat "$JSON_FILE"
