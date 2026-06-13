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
echo "🚀 Starting HP Fan Control Installation"
echo "------------------------------------------"

if [ "$EUID" -ne 0 ]; then
  echo "Please run as root (sudo)."
  exit 1
fi

# --- UPDATE CHECK ---
IS_UPDATE=false
if [ -d "$INSTALL_DIR" ] || [ -L "$SYMLINK_PATH" ]; then
    echo "🔄 Existing installation detected at $INSTALL_DIR"
    read -p "❓ Do you want to UPDATE the application instead of a fresh install? [Y/n] " update_choice

    if [[ "$update_choice" =~ ^[Yy]$ ]] || [[ -z "$update_choice" ]]; then
        IS_UPDATE=true
        echo "Update mode activated. Terminating any running instances of the app..."
        pkill -f "$APP_NAME" 2>/dev/null || true
    else
        echo "Proceeding with a clean installation/overwrite..."
    fi
fi
# --------------------

echo "Step 0: Checking and installing system dependencies..."

# Fedora / RHEL / CentOS
if command -v dnf >/dev/null 2>&1; then
    echo "Detected RPM-based system. Installing via dnf..."
    dnf install -y libayatana-appindicator-gtk3 webkit2gtk4.1 gtk3

# Ubuntu / Debian / Pop!_OS
elif command -v apt-get >/dev/null 2>&1; then
    echo "Detected Debian-based system. Installing via apt..."
    apt-get update
    apt-get install -y libayatana-appindicator3-1 libwebkit2gtk-4.0-37 gir1.2-ayatanaappindicator3-0.1

# Arch Linux / Manjaro
elif command -v pacman >/dev/null 2>&1; then
    echo "Detected Arch-based system. Installing via pacman..."
    pacman -S --noconfirm libayatana-appindicator webkit2gtk

# openSUSE
elif command -v zypper >/dev/null 2>&1; then
    echo "Detected SUSE-based system. Installing via zypper..."
    zypper install -y libayatana-appindicator3-1 webkit2gtk3

else
    echo "Warning: Unsupported package manager. Auto-dependency installation skipped."
fi

echo "Step 1: Configuring permissions and udev rules..."
groupadd -f $GROUP_NAME

if [ -f "./$UDEV_RULE" ]; then
    cp "./$UDEV_RULE" /etc/udev/rules.d/
    udevadm control --reload-rules
    udevadm trigger
    echo "Udev rule installed successfully."
else
    echo "Warning: $UDEV_RULE not found! Hardware permissions might fail."
fi

echo "Step 2: Creating directory, copying files and creating symlink..."
mkdir -p "$INSTALL_DIR"
if [ -d "./hp-fan-control" ]; then
    cp -a ./hp-fan-control/. "$INSTALL_DIR/"
    chmod +x "$INSTALL_DIR/$APP_NAME"

    ln -sf "$INSTALL_DIR/$APP_NAME" "$SYMLINK_PATH"
else
    echo "Error: ./hp-fan-control directory not found! Please build the project first."
    exit 1
fi

if [ -f "./$DESKTOP_FILE" ]; then
    echo "Step 3: Installing desktop entry..."
    cp "./$DESKTOP_FILE" "/tmp/$DESKTOP_FILE"

    sed -i "s|^Exec=.*|Exec=$SYMLINK_PATH|" "/tmp/$DESKTOP_FILE"
    sed -i "s|^Path=.*|Path=$INSTALL_DIR/|" "/tmp/$DESKTOP_FILE"
    sed -i "s|^Icon=.*|Icon=$APP_NAME|" "/tmp/$DESKTOP_FILE"

    mv "/tmp/$DESKTOP_FILE" "$DESKTOP_DIR/"
    chmod 644 "$DESKTOP_DIR/$DESKTOP_FILE"

    ACTUAL_USER=${SUDO_USER:-$USER}
    USER_HOME=$(getent passwd "$ACTUAL_USER" | cut -d: -f6)
    USER_AUTOSTART_DIR="$USER_HOME/.config/autostart"

    # Only ask for autostart if it's a new install or if the desktop file doesn't exist in autostart yet
    if [ "$IS_UPDATE" = true ] && [ -f "$USER_AUTOSTART_DIR/$DESKTOP_FILE" ]; then
        echo "Keeping existing autostart configuration for user '$ACTUAL_USER'."
    else
        echo ""
        read -p "❓ Do you want HP Fan Control to start automatically in the background on login? [Y/n] " setup_autostart

        if [[ "$setup_autostart" =~ ^[Yy]$ ]] || [[ -z "$setup_autostart" ]]; then
            echo "Configuring autostart for user '$ACTUAL_USER'..."

            sudo -u "$ACTUAL_USER" mkdir -p "$USER_AUTOSTART_DIR"
            cp "$DESKTOP_DIR/$DESKTOP_FILE" "$USER_AUTOSTART_DIR/$DESKTOP_FILE"
            sed -i "s|^Exec=.*|Exec=$SYMLINK_PATH --hidden|" "$USER_AUTOSTART_DIR/$DESKTOP_FILE"
            chown "$ACTUAL_USER:$ACTUAL_USER" "$USER_AUTOSTART_DIR/$DESKTOP_FILE"
        else
            echo "Skipping autostart."
            rm -f "$USER_AUTOSTART_DIR/$DESKTOP_FILE"
        fi
    fi
else
    echo "Warning: $DESKTOP_FILE not found."
fi

if [ -f "./$ICON_FILE" ]; then
    echo "Step 4: Deploying icon..."
    cp "./$ICON_FILE" "$ICON_DIR/$ICON_FILE"
fi

echo "Step 5: Automating user group assignment..."
ACTUAL_USER=${SUDO_USER:-$USER}
if [ "$ACTUAL_USER" != "root" ]; then
    usermod -aG $GROUP_NAME "$ACTUAL_USER"
    echo "User '$ACTUAL_USER' has been successfully added to '$GROUP_NAME' group."
else
    echo "Warning: Could not detect actual user. Please run 'sudo usermod -aG $GROUP_NAME \$USER' manually."
fi

echo "------------------------------------------------------------------"
if [ "$IS_UPDATE" = true ]; then
    echo "🎉 Update completed successfully!"
    echo "------------------------------------------------------------------"
    echo "You can now launch HP Fan Control from your application menu."
else
    echo "🎉 Installation completed successfully!"
    echo "------------------------------------------------------------------"
    echo ""
    echo "⚠️  IMPORTANT - REBOOT REQUIRED:"
    echo "We have automatically added your user ('$ACTUAL_USER') to the hardware"
    echo "control group. However, Linux requires a complete system REBOOT"
    echo "for this group permission to take effect."
fi

echo ""
echo "⌨️  KEYBOARD SHORTCUTS (OPTIONAL):"
echo "You can set up custom shortcuts in your Desktop Environment settings"
echo "(e.g., GNOME Settings -> Keyboard -> Custom Shortcuts) using these commands:"
echo ""
echo "  Show/Hide Application UI :"
echo "    hp-fan-control --toggle-ui"
echo ""
echo "  Toggle Fan Mode (Auto/Max/Manual) :"
echo "    hp-fan-control --toggle-mode"
echo ""
echo "------------------------------------------------------------------"
