#!/bin/bash

set -e

cd "$(dirname "$0")/.."

echo "Starting local test build..."

read -p "Please enter the version number (e.g. 1.0.0): " VERSION

if [ -z "$VERSION" ]; then
    echo "❌ Error: Version number cannot be empty!"
    exit 1
fi

TARGET_DIR="local-test-build/HpFanControl-v${VERSION}-linux-x64"

echo "Creating test structure..."
rm -rf "local-test-build"
mkdir -p "$TARGET_DIR/HpFanControl"

echo "Publishing .NET project..."
dotnet publish src/HpFanControl.UI/HpFanControl.UI.csproj -c Release -o "$TARGET_DIR/HpFanControl"

echo "Copying static assets..."
cp deploy-assets/* "$TARGET_DIR/"
chmod +x "$TARGET_DIR/install.sh"
chmod +x "$TARGET_DIR/uninstall.sh"

echo "Local test build is ready for inspection in: $TARGET_DIR/"
