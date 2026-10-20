#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
API_PROJECT="${PROJECT_ROOT}/src/api/ProjectTracker.Api.csproj"
STARTUP_PROJECT="${API_PROJECT}"
MODULE_ORDER="${MigrationModuleOrder:-BoardDbContext}"

if [[ -z "${DATABASE_URL:-}" ]]; then
  echo "DATABASE_URL must be set before running the migration bundle pipeline." >&2
  exit 1
fi

echo "Restoring local .NET tools (dotnet-ef)..."
(cd "${PROJECT_ROOT}" && dotnet tool restore)


CONNECTION_STRING="$(python3 - <<'PY'
import os
from urllib.parse import parse_qsl, unquote, urlparse

uri = os.environ["DATABASE_URL"]
parsed = urlparse(uri)

if parsed.scheme not in {"postgresql", "postgres"}:
    raise SystemExit(f"Unsupported DATABASE_URL scheme: {parsed.scheme}")

username = unquote(parsed.username or "")
password = unquote(parsed.password or "")
database = parsed.path.lstrip("/")
host = parsed.hostname or ""
port = parsed.port or 5432

parts = [
    f"Host={host}",
    f"Port={port}",
    f"Database={database}",
]

if username:
    parts.append(f"Username={username}")
if password:
    parts.append(f"Password={password}")

parts.append("Ssl Mode=Require")

for key, value in parse_qsl(parsed.query, keep_blank_values=True):
    parts.append(f"{key}={value}")

print(";".join(parts))
PY
)"

IFS=':' read -r -a contexts <<< "${MODULE_ORDER}"
if [[ "${#contexts[@]}" -eq 0 ]]; then
  echo "MigrationModuleOrder must contain at least one DbContext name." >&2
  exit 1
fi

for context in "${contexts[@]}"; do
  if [[ -z "${context}" ]]; then
    echo "MigrationModuleOrder contains an empty DbContext entry." >&2
    exit 1
  fi

  bundle_path="${PROJECT_ROOT}/.tmp/${context}.efbundle"
  mkdir -p "${PROJECT_ROOT}/.tmp"

  echo "Building migration bundle for ${context}..."
  dotnet tool run dotnet-ef migrations bundle \
    --project "${API_PROJECT}" \
    --startup-project "${STARTUP_PROJECT}" \
    --configuration Release \
    --context "${context}" \
    --output "${bundle_path}"

  echo "Applying migration bundle for ${context}..."
  "${bundle_path}" --connection "${CONNECTION_STRING}"
done
