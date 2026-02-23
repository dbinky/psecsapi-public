#!/bin/bash
# Fetch the OpenAPI spec from a running PSECS API instance.
# Usage: ./scripts/fetch-openapi.sh [base_url]
#
# Default: http://localhost:5130

BASE_URL="${1:-http://localhost:5130}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
OUTPUT="${SCRIPT_DIR}/../openapi.json"

echo "Fetching OpenAPI spec from ${BASE_URL}/swagger/v1/swagger.json..."
curl -s "${BASE_URL}/swagger/v1/swagger.json" | python3 -m json.tool > "$OUTPUT"

if [ $? -eq 0 ] && [ -s "$OUTPUT" ]; then
  PATHS=$(python3 -c "import json; d=json.load(open('$OUTPUT')); print(len(d.get('paths', {})))")
  SCHEMAS=$(python3 -c "import json; d=json.load(open('$OUTPUT')); print(len(d.get('components', {}).get('schemas', {})))")
  echo "Saved to openapi.json ($PATHS paths, $SCHEMAS schemas)"
else
  echo "ERROR: Failed to fetch OpenAPI spec. Is the API running?"
  rm -f "$OUTPUT"
  exit 1
fi
