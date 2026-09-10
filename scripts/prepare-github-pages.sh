#!/usr/bin/env bash

set -euo pipefail

publish_directory="${1:?Pass the published wwwroot directory as the first argument.}"
repository_name="${2:?Pass the GitHub repository name as the second argument.}"
index_file="${publish_directory}/index.html"

if [[ ! -f "${index_file}" ]]; then
    echo "Published index not found: ${index_file}" >&2
    exit 1
fi

if [[ "${repository_name}" == *.github.io ]]; then
    base_path="/"
else
    base_path="/${repository_name}/"
fi

python3 - "${index_file}" "${base_path}" <<'PY'
from pathlib import Path
import sys

index_file = Path(sys.argv[1])
base_path = sys.argv[2]
contents = index_file.read_text(encoding="utf-8")
source = '<base href="/" />'

if contents.count(source) != 1:
    raise SystemExit(f"Expected exactly one default base element in {index_file}")

index_file.write_text(contents.replace(source, f'<base href="{base_path}" />'), encoding="utf-8")
PY

# GitHub Pages serves 404.html for deep links. Keeping the requested URL lets the
# Blazor router handle that route once the application has loaded.
cp "${index_file}" "${publish_directory}/404.html"
touch "${publish_directory}/.nojekyll"
