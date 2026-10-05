#!/bin/sh
set -eu

ROOT=$(cd "$(dirname "$0")/.." && pwd)
VERSION=$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$ROOT/backend/src/Tily.Host/Tily.Host.csproj")

rm -rf "$ROOT/backend/publish"
dotnet publish "$ROOT/backend/src/Tily.Host/Tily.Host.csproj" -c Release -r osx-arm64 --self-contained -o "$ROOT/backend/publish"

cd "$ROOT/desktop/renderer"
pnpm install --frozen-lockfile
pnpm build

cd "$ROOT/desktop"
pnpm install --frozen-lockfile
npm pkg set version="$VERSION"
pnpm dist

echo "Installeur : $ROOT/desktop/release/Tily-$VERSION-arm64.dmg"
