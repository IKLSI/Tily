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
pnpm build

for ARCH in arm64 x64; do
  rm -rf "$ROOT/backend/publish"
  dotnet publish "$ROOT/backend/src/Tily.Host/Tily.Host.csproj" -c Release -r "osx-$ARCH" --self-contained -p:PublishReadyToRun=true -o "$ROOT/backend/publish"
  pnpm exec electron-builder --mac --publish never "--$ARCH"
  echo "Installeur : $ROOT/desktop/release/Tily-$VERSION-$ARCH.dmg"
done
