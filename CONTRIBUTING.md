# Contributing to KKStudioSocket

[![Build and Test](https://github.com/great-majority/KKStudioSocket/actions/workflows/build.yml/badge.svg)](https://github.com/great-majority/KKStudioSocket/actions/workflows/build.yml)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)

This document provides information for developers who want to contribute to KKStudioSocket or build it from source.

For user documentation, see [README.md](README.md) or [README.ja.md](README.ja.md).

## Overview

KKStudioSocket is a BepInEx plugin compatible with both Koikatsu (KK) and Koikatsu Sunshine (KKS). It features a built-in WebSocket server that allows external applications to control studio operations.

## Supported Games

- **Koikatu (KK)** - .NET Framework 3.5
- **Koikatu Sunshine (KKS)** - .NET Framework 4.6

## Features

- **WebSocket Server** (Default port: 8765, configurable)
- **Real-time Communication** (Using WebSocketSharp library)
- **JSON-based Command Processing** (Using Newtonsoft.Json)
- **Ping-pong Communication** for connectivity verification
- **Transform Operations** for external studio object editing
- **Configurable** server enable/disable toggle

## Building

### Prerequisites

- Visual Studio 2019/2022 (Community or higher)
- .NET Framework 3.5 and 4.6 development tools

### Build Commands

Use the PowerShell script for building:

```powershell
# Basic Release build
.\build.ps1

# Debug build
.\build.ps1 build Debug

# Clean + Restore + Build
.\build.ps1 rebuild

# Build both Debug and Release
.\build.ps1 all

# Clean only
.\build.ps1 clean

# Package restore only
.\build.ps1 restore

# Auto-deploy to game directories
.\build.ps1 deploy                    # Deploy to both games
.\build.ps1 deploy Release both       # Deploy to both games (explicit)
.\build.ps1 deploy Release kk         # Deploy to KK only
.\build.ps1 deploy Release kks        # Deploy to KKS only

# Show help
.\build.ps1 help
```

### Auto-deploy Feature

The `deploy` command automatically copies built DLL files to the game's BepInEx plugin directory:

- **Auto-detection**: Automatically retrieves game installation directories from registry
- **Backup functionality**: Automatically backs up existing files as `.backup` files
- **Error handling**: Displays warnings when games are not found or BepInEx is not installed

Deployment paths:
- **KK**: `[KK Install Directory]\BepInEx\plugins\KK_KKStudioSocket.dll`
- **KKS**: `[KKS Install Directory]\BepInEx\plugins\KKS_KKStudioSocket.dll`

### Building from WSL

When building from WSL environment:

```bash
powershell.exe -ExecutionPolicy Bypass -File "build.ps1" rebuild
```

### Build Artifacts

After successful build, the following files are generated in the `bin/` directory:

- `KK_KKStudioSocket.dll` - For Koikatsu (KK)
- `KKS_KKStudioSocket.dll` - For Koikatsu Sunshine (KKS)

## Installation

1. Copy the corresponding DLL file to the BepInEx `plugins` folder:
   - For KK: `KK_KKStudioSocket.dll`
   - For KKS: `KKS_KKStudioSocket.dll`

2. Start the game and verify that the plugin is loaded

## Configuration

Plugin settings can be changed through the BepInEx Configuration Manager or configuration file:

- **Server.Port** (Default: 8765) - WebSocket server port number
- **Server.Enable** (Default: true) - WebSocket server enable/disable

## WebSocket API

### Connection

Connect to the WebSocket server:
```
ws://127.0.0.1:8765/ws
```

### Commands

#### Ping-Pong Communication (Connectivity Check)

**Request:**
```json
{
  "type": "ping",
  "message": "hello",
  "timestamp": 1234567890
}
```

**Response:**
```json
{
  "type": "pong",
  "message": "hello",
  "timestamp": 1234567890123
}
```

## Development

### Project Structure

```
KKStudioSocket/
├── src/
│   ├── KKStudioSocket.Core/     # Shared code
│   ├── KKStudioSocket.KK/       # KK-specific project
│   └── KKStudioSocket.KKS/      # KKS-specific project
├── build.ps1                   # Build script
├── nuget.config                # NuGet package sources
└── KKStudioSocket.sln          # Solution file
```

### Dependencies

#### Framework
- BepInEx 5.4.22
- IllusionModdingAPI (KKAPI/KKSAPI 1.38.0)
- Harmony 2.9.0

#### External Libraries
- **WebSocketSharp 1.0.3-rc11** - WebSocket communication
- **Newtonsoft.Json 13.0.3** - JSON processing

#### Target Frameworks
- KK: .NET Framework 3.5
- KKS: .NET Framework 4.6

## CI/CD

### GitHub Actions

This project provides an automated CI/CD pipeline using GitHub Actions:

#### Build Workflow (`.github/workflows/build.yml`)
- **Triggers**: Push, Pull Request, Release
- **Jobs**:
  - `build`: Debug/Release matrix build
  - `package`: Automatic packaging on release (includes dependency DLLs)
  - `lint`: Build analysis and code quality checks
- **Artifacts**: 
  - Automatic upload of built DLL files
  - Release packages including dependency DLLs (WebSocketSharp, Newtonsoft.Json)

### CI Environment
- **Runtime**: Windows Latest
- **Build Tools**: MSBuild + NuGet
- **Artifact Retention**: 30 days
- **Dependencies**: Automatic collection and packaging

## References

This project is created with reference to existing MOD projects in the `KK_Plugins` repository. `KK_Plugins` contains numerous MOD implementation examples that are helpful for development:

- **Project Structure**: Three-tier structure of Core/KK/KKS
- **Build Configuration**: Directory.Build.props, nuget.config
- **BepInEx Plugin Implementation Patterns**
- **Various API Usage Examples**

## License

This project is licensed under the **GNU General Public License v3.0**.

This project references the structure and methods of [KK_Plugins](https://github.com/IllusionMods/KK_Plugins), and since KK_Plugins is licensed under GPL v3, the same license is applied as a derivative project.

For details, see the [LICENSE](LICENSE) file.

### Important Points

- This software is provided without warranty
- When distributing source code, the same GPL v3 license must be applied
- Commercial use is allowed but must comply with GPL v3 conditions

For detailed license information, see [https://www.gnu.org/licenses/gpl-3.0.html](https://www.gnu.org/licenses/gpl-3.0.html).

## Contributing

Contributions to the project are welcome. Please submit pull requests or report issues.

## Disclaimer

- This plugin is under development, so functionality and specifications may change
- Use at your own risk