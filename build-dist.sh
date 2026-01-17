#!/usr/bin/env bash
set -euo pipefail

root_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
project_dir="$root_dir/TLDAccessibility"
dist_dir="$root_dir/dist/TLDAccessibility"

rm -rf "$dist_dir"
mkdir -p "$dist_dir"

dotnet build "$project_dir/TLDAccessibility.csproj" -c Release

output_dir="$project_dir/bin/Release/net6.0-windows"

if [[ ! -f "$output_dir/TLDAccessibility.dll" ]]; then
  echo "Build output not found: $output_dir/TLDAccessibility.dll" >&2
  exit 1
fi

cp "$output_dir/TLDAccessibility.dll" "$dist_dir/"

if [[ -f "$output_dir/Tolk.dll" ]]; then
  cp "$output_dir/Tolk.dll" "$dist_dir/"
elif [[ -f "$project_dir/Interop/Tolk.dll" ]]; then
  cp "$project_dir/Interop/Tolk.dll" "$dist_dir/"
fi

cp "$root_dir/Mods/TLDAccessibility/settings.json" "$dist_dir/"
cp "$root_dir/Mods/TLDAccessibility/settings.schema.md" "$dist_dir/"
cp "$root_dir/README.md" "$dist_dir/"

echo "Dist package created at $dist_dir"
