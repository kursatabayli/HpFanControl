#!/bin/bash

# Configuration
APP_NAME="hp-fan-control"
INSTALL_DIR="/opt/$APP_NAME"
SYMLINK_PATH="/usr/local/bin/$APP_NAME"
DESKTOP_DIR="/usr/share/applications"
ICON_DIR="/usr/share/pixmaps"
DESKTOP_FILE="$APP_NAME.desktop"
ICON_FILE="$APP_NAME.png"
GROUP_NAME="fancontrol"
UDEV_RULE="99-fancontrol.rules"

echo "------------------------------------------"
echo "🗑️ Starting HP Fan Control Uninstallation"
echo "------------------------------------------"

if [ "$EUID" -ne 0 ]; then
  echo "Please run as root (sudo)."
  exit 1
fi

echo "Step 1: Removing application files and symlink..."
if [ -d "$INSTALL_DIR" ]; then
    rm -rf "$INSTALL_DIR"
fi
if [ -L "$SYMLINK_PATH" ] || [ -f "$SYMLINK_PATH" ]; then
    rm -f "$SYMLINK_PATH"
fi

echo "Step 2: Removing desktop and autostart entries..."
if [ -f "$DESKTOP_DIR/$DESKTOP_FILE" ]; then
    rm -f "$DESKTOP_DIR/$DESKTOP_FILE"
fi

ACTUAL_USER=${SUDO_USER:-$USER}
USER_HOME=$(getent passwd "$ACTUAL_USER" | cut -d: -f6)
USER_AUTOSTART_DIR="$USER_HOME/.config/autostart"

if [ -f "$USER_AUTOSTART_DIR/$DESKTOP_FILE" ]; then
    rm -f "$USER_AUTOSTART_DIR/$DESKTOP_FILE"
fi

echo "Step 3: Removing icon..."
if [ -f "$ICON_DIR/$ICON_FILE" ]; then
    rm -f "$ICON_DIR/$ICON_FILE"
fi

echo "Step 4: Removing udev rules and resetting permissions..."
if [ -f "/etc/udev/rules.d/$UDEV_RULE" ]; then
    rm -f "/etc/udev/rules.d/$UDEV_RULE"
    udevadm control --reload-rules
    udevadm trigger
fi

echo "Step 5: Removing user group '$GROUP_NAME'..."
if getent group "$GROUP_NAME" > /dev/null; then
    groupdel "$GROUP_NAME"
fi

echo "------------------------------------------"
echo "✨ Uninstallation finished successfully. Your system is clean."
echo "------------------------------------------"
