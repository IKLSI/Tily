#!/bin/sh
set -eu

ROOT=$(cd "$(dirname "$0")/.." && pwd)
VERSION=$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$ROOT/backend/src/Tily.Host/Tily.Host.csproj")

cd "$ROOT/desktop/renderer"
pnpm install --frozen-lockfile
pnpm build

cd "$ROOT/desktop"
pnpm install --frozen-lockfile
npm pkg set version="$VERSION"

for ARCH in arm64 x64; do
  rm -rf "$ROOT/backend/publish"
  dotnet publish "$ROOT/backend/src/Tily.Host/Tily.Host.csproj" -c Release -r "osx-$ARCH" --self-contained -o "$ROOT/backend/publish"
  pnpm dist "--$ARCH"
  echo "Installeur : $ROOT/desktop/release/Tily-$VERSION-$ARCH.dmg"
done
