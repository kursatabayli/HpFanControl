#!/bin/bash

# Configuration
APP_NAME="HpFanControl"
OLD_APP_NAME="hp-fan-control"

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
  echo "Please run as root (sudo/pkexec)."
  exit 1
fi

# --- AUTO UPDATE CHECK ---
IS_AUTO_UPDATE=false
if [ "$1" == "--auto-update" ]; then
    IS_AUTO_UPDATE=true
    echo "⚡ Auto-update mode active. Proceeding silently..."
fi

# Determine the actual user early on for display launching later
ACTUAL_USER=${SUDO_USER:-$USER}
if [ "$ACTUAL_USER" == "root" ]; then
    # Try to extract the user who ran pkexec
    ACTUAL_USER=$(logname 2>/dev/null || echo $USER)
fi
USER_HOME=$(getent passwd "$ACTUAL_USER" | cut -d: -f6)

# --- MIGRATION & CLEANUP ---
OLD_INSTALL_DIR="/opt/$OLD_APP_NAME"
OLD_SYMLINK="/usr/local/bin/$OLD_APP_NAME"
OLD_CONFIG_DIR="$USER_HOME/.config/$OLD_APP_NAME"
NEW_CONFIG_DIR="$USER_HOME/.config/$APP_NAME"
LEGACY_AUTOSTART_FOUND=false

echo "Step 0: Checking for legacy installations and migrating data..."

pkill -f "$OLD_APP_NAME" 2>/dev/null || true
pkill -f "$APP_NAME" 2>/dev/null || true
sleep 1

if [ -d "$OLD_CONFIG_DIR" ]; then
    echo "🔄 Legacy configuration found. Migrating to new location..."
    sudo -u "$ACTUAL_USER" mkdir -p "$NEW_CONFIG_DIR"
    sudo -u "$ACTUAL_USER" cp -n "$OLD_CONFIG_DIR"/* "$NEW_CONFIG_DIR"/ 2>/dev/null || true
    rm -rf "$OLD_CONFIG_DIR"
    echo "✅ Configuration migration successful."
fi

if [ -d "$OLD_INSTALL_DIR" ]; then
    rm -rf "$OLD_INSTALL_DIR"
fi
if [ -L "$OLD_SYMLINK" ]; then
    rm -f "$OLD_SYMLINK"
fi
if [ -f "$DESKTOP_DIR/$OLD_APP_NAME.desktop" ]; then
    rm -f "$DESKTOP_DIR/$OLD_APP_NAME.desktop"
fi

if [ -f "$USER_HOME/.config/autostart/$OLD_APP_NAME.desktop" ]; then
    LEGACY_AUTOSTART_FOUND=true
    rm -f "$USER_HOME/.config/autostart/$OLD_APP_NAME.desktop"
fi
# -----------------------------------------------------

# --- UPDATE CHECK ---
IS_UPDATE=false
if [ -d "$INSTALL_DIR" ] || [ -L "$SYMLINK_PATH" ]; then
    if [ "$IS_AUTO_UPDATE" = true ]; then
        IS_UPDATE=true
    else
        echo "🔄 Existing installation detected at $INSTALL_DIR"
        read -p "❓ Do you want to UPDATE the application instead of a fresh install? [Y/n] " update_choice

        if [[ "$update_choice" =~ ^[Yy]$ ]] || [[ -z "$update_choice" ]]; then
            IS_UPDATE=true
        else
            echo "Proceeding with a clean installation/overwrite..."
        fi
    fi
fi
# --------------------

echo "Step 1: Checking and installing system dependencies..."

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

echo "Step 2: Configuring permissions and udev rules..."
groupadd -f $GROUP_NAME

if [ -f "./$UDEV_RULE" ]; then
    cp "./$UDEV_RULE" /etc/udev/rules.d/
    udevadm control --reload-rules
    udevadm trigger
    echo "Udev rule installed successfully."
else
    echo "Warning: $UDEV_RULE not found! Hardware permissions might fail."
fi

echo "Step 3: Creating directory, copying files and creating symlink..."
mkdir -p "$INSTALL_DIR"
if [ -d "./$APP_NAME" ]; then
    cp -a "./$APP_NAME/." "$INSTALL_DIR/"
    chmod +x "$INSTALL_DIR/$APP_NAME"

    ln -sf "$INSTALL_DIR/$APP_NAME" "$SYMLINK_PATH"
else
    echo "Error: ./$APP_NAME directory not found! Please build the project first."
    exit 1
fi

if [ -f "./$DESKTOP_FILE" ]; then
    echo "Step 4: Installing desktop entry..."
    cp "./$DESKTOP_FILE" "/tmp/$DESKTOP_FILE"

    sed -i "s|^Exec=.*|Exec=$SYMLINK_PATH|" "/tmp/$DESKTOP_FILE"
    sed -i "s|^Path=.*|Path=$INSTALL_DIR/|" "/tmp/$DESKTOP_FILE"
    sed -i "s|^Icon=.*|Icon=$APP_NAME|" "/tmp/$DESKTOP_FILE"

    mv "/tmp/$DESKTOP_FILE" "$DESKTOP_DIR/"
    chmod 644 "$DESKTOP_DIR/$DESKTOP_FILE"

    USER_AUTOSTART_DIR="$USER_HOME/.config/autostart"

    if [ -f "$USER_AUTOSTART_DIR/$DESKTOP_FILE" ] || [ "$LEGACY_AUTOSTART_FOUND" = true ]; then
        echo "Updating existing autostart configuration for user '$ACTUAL_USER'."
        sudo -u "$ACTUAL_USER" mkdir -p "$USER_AUTOSTART_DIR"
        cp "$DESKTOP_DIR/$DESKTOP_FILE" "$USER_AUTOSTART_DIR/$DESKTOP_FILE"
        sed -i "s|^Exec=.*|Exec=$SYMLINK_PATH --hidden|" "$USER_AUTOSTART_DIR/$DESKTOP_FILE"
        chown "$ACTUAL_USER:$ACTUAL_USER" "$USER_AUTOSTART_DIR/$DESKTOP_FILE"
        
    elif [ "$IS_AUTO_UPDATE" = false ]; then
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
    echo "Step 5: Deploying icon..."
    cp "./$ICON_FILE" "$ICON_DIR/$ICON_FILE"
fi

echo "Step 6: Automating user group assignment..."
if [ "$ACTUAL_USER" != "root" ]; then
    usermod -aG $GROUP_NAME "$ACTUAL_USER"
    echo "User '$ACTUAL_USER' has been successfully added to '$GROUP_NAME' group."
else
    echo "Warning: Could not detect actual user. Please run 'sudo usermod -aG $GROUP_NAME \$USER' manually."
fi

echo "------------------------------------------------------------------"
if [ "$IS_UPDATE" = true ]; then
    echo "Update completed successfully!"
    echo "------------------------------------------------------------------"
    
    if [ "$IS_AUTO_UPDATE" = true ] && [ "$ACTUAL_USER" != "root" ]; then
        echo "🔄 Auto-restarting application for user '$ACTUAL_USER'..."
        
        USER_UID=$(id -u "$ACTUAL_USER")
        sudo -u "$ACTUAL_USER" env XDG_RUNTIME_DIR=/run/user/$USER_UID DISPLAY=${DISPLAY:-:0} nohup "$SYMLINK_PATH" > /dev/null 2>&1 &
    else
        echo "You can now launch HP Fan Control from your application menu."
    fi
else
    echo "Installation completed successfully!"
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
echo "    HpFanControl --toggle-ui"
echo ""
echo "  Toggle Fan Mode (Auto/Max/Manual) :"
echo "    HpFanControl --toggle-mode"
echo ""
echo "------------------------------------------------------------------"