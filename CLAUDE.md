
# KKStudioSocket Project Information

- `README.md` contains English documentation for users, `README.ja.md` contains Japanese documentation for users. When updating either file, simultaneously update both language versions.
- Developer information should be documented in `CONTRIBUTING.md`.
- Model design and API specifications are documented in `MODELS.md`.
- After making code changes, run builds to ensure there are no issues.
- Write code comments in English.
- Write commit messages in the format: {emoji English commit message}.

## Build Instructions

### Building from WSL (Recommended)

Since Claude is running from WSL, remember that you need to use powershell.exe for various build commands.

```bash
powershell.exe -ExecutionPolicy Bypass -File "build.ps1" [target] [configuration]
```

### Build Targets
- `build` (default) - Build with specified configuration
- `rebuild` - Clean + restore + build
- `all` - Build both Debug and Release
- `clean` - Clean build artifacts
- `restore` - Restore NuGet packages
- `deploy` - Auto-deploy to game directory
- `help` - Show help

### Configurations
- `Release` (default) - Release build
- `Debug` - Debug build

### Usage Examples
```bash
# Basic Release build
powershell.exe -ExecutionPolicy Bypass -File "build.ps1"

# Debug build
powershell.exe -ExecutionPolicy Bypass -File "build.ps1" build Debug

# Full rebuild
powershell.exe -ExecutionPolicy Bypass -File "build.ps1" rebuild

# Auto-deploy to game directory
powershell.exe -ExecutionPolicy Bypass -File "build.ps1" deploy
powershell.exe -ExecutionPolicy Bypass -File "build.ps1" deploy Release kk
powershell.exe -ExecutionPolicy Bypass -File "build.ps1" deploy Release kks
```

### Deploy Command Details
- Auto-detect game installation directory from registry
- Deploy DLL to `[GameDirectory]\BepInEx\plugins\`
- Automatically backup existing files as `.backup`
- Error handling (game not found, BepInEx not installed, etc.)

## Project Structure
- `src/KKStudioSocket.Core/` - Shared code (shared project)
- `src/KKStudioSocket.KK/` - KK-specific project (.NET Framework 3.5)
- `src/KKStudioSocket.KKS/` - KKS-specific project (.NET Framework 4.6)
- `bin/` - Build output directory

## Build Artifacts
- `KK_KKStudioSocket.dll` - For Koikatsu (KK) (~57KB)
- `KKS_KKStudioSocket.dll` - For Koikatsu Sunshine (KKS) (~57KB)

## Development Environment
- Visual Studio 2019/2022
- BepInEx dependency packages (auto-retrieved from custom NuGet feed)

## Dependencies

### Framework
- **BepInEx** 5.4.22 - MOD framework
- **IllusionModdingAPI**
  - KKAPI 1.38.0 (for KK)
  - KKSAPI 1.38.0 (for KKS)
- **Harmony** 2.9.0 - Runtime patching

### External Libraries
- **WebSocketSharp** 1.0.3-rc11
  - WebSocket communication implementation
  - .NET Framework 3.5/4.6 compatible
  - Server functionality, client connection management
- **Newtonsoft.Json** 13.0.3
  - JSON serialization/deserialization
  - Command processing, response generation
  - .NET Framework 3.5/4.6 compatible

## Features

### WebSocket Server
- Port: 8765 (default, configurable)
- Endpoint: `/ws`
- Connection URL: `ws://127.0.0.1:8765/ws`
- Client connection/disconnection logging
- Error handling

### Command Processing
- **Ping-Pong Communication** - Connection verification
- **Transform Operations** - Studio item control
- Command type detection system
- Safe processing on main thread

## Testing

### Build Testing
```bash
# Package restore and build
powershell.exe -ExecutionPolicy Bypass -File "build.ps1" restore
powershell.exe -ExecutionPolicy Bypass -File "build.ps1" build Release
```

## Troubleshooting

### Build Errors
- **WebSocketSharp not found**: Restore NuGet packages with `restore` command
- **Newtonsoft.Json error**: Version 13.0.3 required
- **.NET Framework version error**: Check difference between KK(3.5) and KKS(4.6)

### Runtime Errors
- **Port in use**: Change port number or terminate other applications
- **JSON parsing error**: Verify command format
- **BepInEx logs**: Check `LogOutput.log` for error details
