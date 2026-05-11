# WinPowerTray

[![CI](https://github.com/hello-world-dot-c/WinPowerTray/actions/workflows/ci.yml/badge.svg)](https://github.com/hello-world-dot-c/WinPowerTray/actions/workflows/ci.yml)
[![Latest release](https://img.shields.io/github/v/release/hello-world-dot-c/WinPowerTray)](https://github.com/hello-world-dot-c/WinPowerTray/releases/latest)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

Switch Windows 11 power modes from the system tray — no Settings app, no clicks through menus, no admin rights needed.

## Features

- **Three power modes** accessible from the tray menu:
  - 🍃 Best power efficiency — maximise battery life
  - ⚖️ Balanced — the Windows default
  - ⚡ Best performance — maximum CPU/GPU throughput
- **Left- and right-click both open the menu** — no need to remember which one
- **Two label styles** to match what Windows Settings shows on your machine:
  - *Best power efficiency / Balanced / Best performance* (most desktops)
  - *Recommended / Better Performance / Best Performance* (some laptops, e.g. Intel Evo)
- **Colour-coded tray icon** — green / blue / red at a glance
- **Toast notification** on every mode change (can be disabled in Settings)
- **Stays in sync** with external changes made via Settings or `powercfg`
- **Settings persisted** in `%AppData%\WinPowerTray\settings.json`
- **Single-instance guard** — safe to add to startup
- **No elevation required** — uses the official Windows power overlay API

## Requirements

- Windows 11 (or Windows 10 version 2004 / build 19041 or later)
- The installer is self-contained — no .NET runtime download needed

## Installation

1. Download **`WinPowerTray-x.y.z-Setup.exe`** from the [latest release](https://github.com/hello-world-dot-c/WinPowerTray/releases/latest).
2. Run the installer. No administrator rights are required.
3. Tick **"Start WinPowerTray automatically with Windows"** if you want it in the startup.
4. The app launches in the system tray immediately after installation.

## Usage

Click the tray icon (left or right) to open the menu:

| Menu item | Action |
|-----------|--------|
| 🍃 Best power efficiency | Activates efficiency mode |
| ⚖️ Balanced | Activates balanced mode (default) |
| ⚡ Best performance | Activates performance mode |
| Settings ▸ Label style | Choose which label set matches your Windows version |
| Settings ▸ Show power mode change notification | Toggle the toast shown on every mode change |
| Go to project page | Opens this page in your browser |
| Exit | Quits WinPowerTray |

A checkmark shows the currently active mode. By default a toast confirms every switch (including changes made from Windows Settings or `powercfg`).

### Label style

Windows Settings uses different names for the same three overlays depending on the machine. If the labels in WinPowerTray don't match what your Settings app shows — or if switching to the middle/leaf mode doesn't seem to work — open **Settings ▸ Label style** and pick the set that matches your system. The mapping (label *and* underlying overlay GUID) updates immediately and is persisted for next launch.

## Building from source

**Prerequisites:** [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)

```powershell
git clone https://github.com/hello-world-dot-c/WinPowerTray.git
cd WinPowerTray
dotnet build WinPowerTray/WinPowerTray.csproj --configuration Release
```

The executable is in `WinPowerTray/bin/Release/net8.0-windows/WinPowerTray.exe`.

### Building the installer locally

**Additional prerequisite:** [Inno Setup 6](https://jrsoftware.org/isinfo.php)

```powershell
# 1. Publish a self-contained single-file binary
dotnet publish WinPowerTray/WinPowerTray.csproj `
  --configuration Release --runtime win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  --output publish

# 2. Build the installer (output goes to installer/output/)
iscc /DAppVersion=1.0.0 installer\WinPowerTray.iss
```

## Releasing a new version

The release process is fully automated:

1. Go to **Actions → Version bump** and choose `patch`, `minor`, or `major`.
2. The workflow bumps the version in the `.csproj`, commits, and pushes a `vX.Y.Z` tag.
3. The **Release** workflow fires automatically on that tag: it builds the self-contained binary, packages the installer with Inno Setup, and publishes a GitHub release with the installer attached.

## CI / CD overview

| Workflow | Trigger | What it does |
|----------|---------|--------------|
| `ci.yml` | Push / PR to `main` | Builds and publishes (self-contained) to verify the project compiles |
| `release.yml` | Push of a `vX.Y.Z` tag | Builds installer, creates GitHub release |
| `version-bump.yml` | Manual (workflow dispatch) | Bumps csproj version, commits, tags |

## License

[MIT](LICENSE)
