#!/usr/bin/env bash
set -euo pipefail

root_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
project_dir="$root_dir/TLDAccessibility"
dist_dir="$root_dir/dist"
mods_dir="$dist_dir/Mods"
user_data_dir="$dist_dir/UserData/TLDAccessibility"

rm -rf "$dist_dir"
mkdir -p "$mods_dir" "$user_data_dir"

dotnet build "$project_dir/TLDAccessibility.csproj" -c Release

output_dir="$project_dir/bin/Release/net6.0-windows"

if [[ ! -f "$output_dir/TLDAccessibility.dll" ]]; then
  echo "Build output not found: $output_dir/TLDAccessibility.dll" >&2
  exit 1
fi

cp "$output_dir/TLDAccessibility.dll" "$mods_dir/"

if [[ -f "$output_dir/Tolk.dll" ]]; then
  cp "$output_dir/Tolk.dll" "$mods_dir/"
elif [[ -f "$project_dir/Interop/Tolk.dll" ]]; then
  cp "$project_dir/Interop/Tolk.dll" "$mods_dir/"
fi

cp "$root_dir/Mods/TLDAccessibility/settings.json" "$user_data_dir/"
cp "$root_dir/Mods/TLDAccessibility/settings.schema.md" "$user_data_dir/"
cp "$root_dir/README.md" "$dist_dir/"

echo "Dist package created at $dist_dir"
