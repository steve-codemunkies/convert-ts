#!/usr/bin/env bash
# install.sh — Install or update the convert-ts bash functions.
#
# Downloads convert-ts.sh from GitHub and places it in an installation
# directory, then optionally registers it in the user's shell profile.
#
# No git or manual file copying required.
#
# Usage:
#   bash <(curl -fsSL https://raw.githubusercontent.com/steve-codemunkies/convert-ts/main/bash/install.sh)
#
# Options:
#   --branch <name>       Install from this branch (default: main)
#   --dir <path>          Install directory (default: ~/.local/share/convert-ts)
#   --add-to-profile      Automatically add a source line to the shell profile
#   --profile <path>      Explicit shell profile path (overrides auto-detection)
#   --force               Overwrite existing files without prompting
#   --help                Show this help message
#
# Environment:
#   CONVERT_TS_INSTALL_DIR   Override the default install directory.
#
# Examples:
#   # Minimal install (download only)
#   bash <(curl -fsSL https://raw.githubusercontent.com/steve-codemunkies/convert-ts/main/bash/install.sh)
#
#   # Install and add to profile automatically
#   bash <(curl -fsSL https://raw.githubusercontent.com/steve-codemunkies/convert-ts/main/bash/install.sh) --add-to-profile
#
#   # Install from a specific branch
#   bash <(curl -fsSL https://raw.githubusercontent.com/steve-codemunkies/convert-ts/main/bash/install.sh) --branch develop --add-to-profile

set -euo pipefail

# ---------------------------------------------------------------------------
# Defaults
# ---------------------------------------------------------------------------
_BRANCH="main"
_INSTALL_DIR="${CONVERT_TS_INSTALL_DIR:-${HOME}/.local/share/convert-ts}"
_ADD_TO_PROFILE=0
_PROFILE_PATH=""
_FORCE=0

_REPO_OWNER="steve-codemunkies"
_REPO_NAME="convert-ts"
_SCRIPT_FILE="convert-ts.sh"

# ---------------------------------------------------------------------------
# Argument parsing
# ---------------------------------------------------------------------------
while [[ $# -gt 0 ]]; do
    case "$1" in
        --branch)
            _BRANCH="$2"; shift 2 ;;
        --dir)
            _INSTALL_DIR="$2"; shift 2 ;;
        --add-to-profile)
            _ADD_TO_PROFILE=1; shift ;;
        --profile)
            _PROFILE_PATH="$2"; shift 2 ;;
        --force)
            _FORCE=1; shift ;;
        --help|-h)
            sed -n '/^# /p' "$0" | sed 's/^# //'
            exit 0 ;;
        *)
            echo "install.sh: unknown option '$1'" >&2
            exit 1 ;;
    esac
done

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------
_info()  { echo "[convert-ts install] $*"; }
_error() { echo "[convert-ts install] ERROR: $*" >&2; exit 1; }

_detect_profile() {
    # Return the most appropriate shell profile file for the current user.
    # Priority: $BASH_PROFILE, $PROFILE, common file names.
    if [[ -n "$_PROFILE_PATH" ]]; then
        echo "$_PROFILE_PATH"; return
    fi
    local candidates=(
        "${HOME}/.bashrc"
        "${HOME}/.bash_profile"
        "${HOME}/.profile"
        "${HOME}/.zshrc"
    )
    for f in "${candidates[@]}"; do
        if [[ -f "$f" ]]; then
            echo "$f"; return
        fi
    done
    # Fallback to .bashrc (will be created if needed)
    echo "${HOME}/.bashrc"
}

_download() {
    local url="$1"
    local dest="$2"
    if command -v curl >/dev/null 2>&1; then
        curl -fsSL "$url" -o "$dest"
    elif command -v wget >/dev/null 2>&1; then
        wget -qO "$dest" "$url"
    else
        _error "Neither curl nor wget is available. Please install one and retry."
    fi
}

# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

_RAW_BASE="https://raw.githubusercontent.com/${_REPO_OWNER}/${_REPO_NAME}/${_BRANCH}/bash"
_SCRIPT_URL="${_RAW_BASE}/${_SCRIPT_FILE}"
_DEST="${_INSTALL_DIR}/${_SCRIPT_FILE}"
_SOURCE_LINE="source \"${_DEST}\""

# Create install directory if needed.
if [[ ! -d "$_INSTALL_DIR" ]]; then
    _info "Creating install directory: ${_INSTALL_DIR}"
    mkdir -p "$_INSTALL_DIR"
fi

# Check for existing installation.
if [[ -f "$_DEST" && "$_FORCE" -eq 0 ]]; then
    _info "Updating existing installation at ${_DEST}"
else
    _info "Installing to ${_DEST}"
fi

# Download the script.
_info "Downloading ${_SCRIPT_URL} ..."
_download "$_SCRIPT_URL" "$_DEST"
chmod +x "$_DEST"
_info "Script installed: ${_DEST}"

# Profile registration.
if [[ "$_ADD_TO_PROFILE" -eq 1 ]]; then
    local_profile=$(_detect_profile)
    if grep -qF "$_SOURCE_LINE" "$local_profile" 2>/dev/null; then
        _info "Profile ${local_profile} already contains the source line — skipping."
    else
        _info "Adding source line to ${local_profile}"
        {
            echo ""
            echo "# convert-ts — Unix timestamp conversion functions"
            echo "${_SOURCE_LINE}"
        } >> "$local_profile"
        _info "Done. Open a new terminal or run: source ${local_profile}"
    fi
else
    _info ""
    _info "To use the functions in your shell, add the following line to your"
    _info "shell profile (e.g. ~/.bashrc or ~/.bash_profile):"
    _info ""
    _info "    ${_SOURCE_LINE}"
    _info ""
    _info "Then reload your profile with: source ~/.bashrc"
    _info ""
    _info "Or re-run this script with --add-to-profile to do it automatically."
fi
