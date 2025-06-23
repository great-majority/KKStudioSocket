# KKStudioSocket

[![Build and Test](https://github.com/great-majority/KKStudioSocket/actions/workflows/build.yml/badge.svg)](https://github.com/great-majority/KKStudioSocket/actions/workflows/build.yml)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)

A Koikatsu plugin that provides a WebSocket server for external control of Studio objects.

[日本語ドキュメント (Japanese Documentation)](README.ja.md) | [Development Guide](CONTRIBUTING.md)

## 🎮 What is KKStudioSocket?

KKStudioSocket allows you to control Koikatsu Studio objects remotely via WebSocket connections. You can:

- **Add objects** (items, characters, lights) to your scene
- **Modify object properties** (position, rotation, scale)
- **Retrieve scene structure** as a hierarchical tree
- **Control the scene** programmatically from external applications

## 📋 Requirements

- **Koikatsu (KK)** or **Koikatsu Sunshine (KKS)**
- **BepInEx 5.4.21+** installed
- **IllusionModdingAPI** (KKAPI/KKSAPI) installed

## 🚀 Installation

1. **Download** the latest release from [Releases](https://github.com/great-majority/KKStudioSocket/releases)
2. **Extract** the appropriate DLL file:
   - For Koikatsu: `KK_KKStudioSocket.dll`
   - For Koikatsu Sunshine: `KKS_KKStudioSocket.dll`
3. **Copy** the DLL to your game's `BepInEx/plugins/` folder
4. **Start** the game and enter Studio mode

## ⚙️ Configuration

The plugin creates a configuration file that can be modified through BepInEx Configuration Manager:

- **Server Port** (Default: 8765) - WebSocket server port
- **Server Enable** (Default: true) - Enable/disable the WebSocket server

## 🔗 Connection

Connect to the WebSocket server using any WebSocket client:

```
ws://127.0.0.1:8765/ws
```

All commands and responses use JSON format.

## 📡 Available Commands

### Table of Contents
- [🏓 Ping-Pong (Connection Test)](#-ping-pong-connection-test)
- [🌲 Tree (Scene Structure)](#-tree-scene-structure)
- [🔄 Update (Object Properties)](#-update-object-properties)
  - [Update Transform](#update-transform)
  - [Update Item Color](#update-item-color)
- [➕ Add (Object Creation)](#-add-object-creation)
  - [Add Items](#add-items)
  - [Add Lights](#add-lights)
  - [Add Characters](#add-characters)
  - [Add Folders](#add-folders)
  - [Add Cameras](#add-cameras)
- [🌳 Hierarchy (Object Relationships)](#-hierarchy-object-relationships)
  - [Attach Object](#attach-object)
  - [Detach Object](#detach-object)
- [🗑️ Delete (Object Deletion)](#️-delete-object-deletion)
- [🎥 Camera (Viewport Control)](#-camera-viewport-control)
  - [Set Current View](#set-current-view)
  - [Switch to Camera Object](#switch-to-camera-object)
  - [Switch to Free Camera](#switch-to-free-camera)
  - [Get Current View](#get-current-view)

### 🏓 Ping-Pong (Connection Test)

Test connection and latency:

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

### 🌳 Tree (Get Scene Structure)

Retrieve the complete scene object hierarchy:

**Request:**
```json
{
  "type": "tree"
}
```

**Response:**
```json
[
  {
    "name": "Item Name",
    "objectInfo": {
      "id": 12345,
      "type": "OCIItem"
    },
    "children": [...]
  }
]
```

### 📝 Update (Modify Objects)

#### Update Transform

Change position, rotation, or scale of existing objects:

**Request:**
```json
{
  "type": "update",
  "command": "transform",
  "id": 12345,
  "pos": [0.0, 1.0, 0.0],
  "rot": [0.0, 90.0, 0.0],
  "scale": [1.0, 1.0, 1.0]
}
```

- `id`: Object ID (obtained from tree command)
- `pos`: Position [X, Y, Z] (optional)
- `rot`: Rotation [X, Y, Z] in degrees (optional)
- `scale`: Scale [X, Y, Z] (optional)

**Response (Success):**
```json
{
  "type": "success",
  "message": "Transform updated for object ID 12345"
}
```

#### Update Item Color

Change item colors and transparency (items only):

**Request (Change specific color):**
```json
{
  "type": "update",
  "command": "color",
  "id": 12345,
  "color": [1.0, 0.0, 0.0],
  "colorIndex": 0
}
```

**Request (Change color with alpha):**
```json
{
  "type": "update",
  "command": "color",
  "id": 12345,
  "color": [0.0, 1.0, 0.0, 0.8],
  "colorIndex": 1
}
```

**Request (Change overall transparency):**
```json
{
  "type": "update",
  "command": "color",
  "id": 12345,
  "alpha": 0.5
}
```

**Request (Change both color and transparency):**
```json
{
  "type": "update",
  "command": "color",
  "id": 12345,
  "color": [0.0, 0.0, 1.0],
  "colorIndex": 0,
  "alpha": 0.7
}
```

- `id`: Item ID to update
- `color`: RGB [r, g, b] or RGBA [r, g, b, a] values (0.0-1.0) (optional)
- `colorIndex`: Color slot index (0-7) (required when using color)
  - 0-2: Main colors 1-3
  - 3-5: Pattern colors 1-3
  - 6: Shadow color
  - 7: Glass/Alpha color
- `alpha`: Overall transparency (0.0-1.0) (optional)

**Response (Success):**
```json
{
  "type": "success",
  "message": "Color updated for item ID 12345"
}
```

**Response (Error - Not an item):**
```json
{
  "type": "error",
  "message": "Object with ID 12345 is not an item. Color can only be changed for items."
}
```

**Response (Error):**
```json
{
  "type": "error",
  "message": "Object with ID 12345 not found"
}
```

### ➕ Add (Create Objects)

#### Add Items

Add items to the scene:

**Request:**
```json
{
  "type": "add",
  "command": "item",
  "group": 0,
  "category": 0,
  "itemId": 1
}
```

- `group`: Item group ID
- `category`: Item category ID
- `itemId`: Item number

**Response (Success):**
```json
{
  "type": "success",
  "message": "Item added successfully: group=0, category=0, no=1"
}
```

**Response (Error):**
```json
{
  "type": "error",
  "message": "Invalid item parameters: group=-1, category=0, no=1"
}
```

#### Add Lights

Add lights to the scene:

**Request:**
```json
{
  "type": "add",
  "command": "light",
  "lightId": 0
}
```

- `lightId`: Light type (0=Directional, 1=Point, 2=Spot)

**Response (Success):**
```json
{
  "type": "success",
  "message": "Light added successfully: no=0"
}
```

**Response (Error):**
```json
{
  "type": "error",
  "message": "Light limit reached or light check disabled"
}
```

#### Add Characters

Add characters to the scene:

**Request:**
```json
{
  "type": "add",
  "command": "character",
  "sex": "female",
  "path": "C:/path/to/character.png"
}
```

- `sex`: "female" or "male"
- `path`: Absolute path to character file (.png)

**Response (Success):**
```json
{
  "type": "success",
  "message": "Female character added successfully: C:/path/to/character.png"
}
```

**Response (Error):**
```json
{
  "type": "error",
  "message": "Character file not found: C:/path/to/character.png"
}
```

#### Add Folders

Add folders to the scene for organizing objects:

**Request:**
```json
{
  "type": "add",
  "command": "folder",
  "name": "My Folder"
}
```

- `name`: Folder name (optional, defaults to "フォルダー")

**Response (Success):**
```json
{
  "type": "success",
  "message": "Folder added successfully with name: My Folder"
}
```

**Response (Error):**
```json
{
  "type": "error",
  "message": "Add folder error: [error details]"
}
```

#### Add Cameras

Add camera objects to the scene:

**Request:**
```json
{
  "type": "add",
  "command": "camera"
}
```

**Request (with name):**
```json
{
  "type": "add",
  "command": "camera",
  "name": "Main Camera"
}
```

**Response (Success):**
```json
{
  "type": "success",
  "message": "Camera added successfully",
  "objectId": 12345
}
```

### 🌳 Hierarchy (Object Relationships)

Manage parent-child relationships between objects:

#### Attach Object

Attach an object to another object (folders, items, characters can be parents):

**Request:**
```json
{
  "type": "hierarchy",
  "command": "attach",
  "childId": 12345,
  "parentId": 67890
}
```

- `childId`: ID of the object to be attached
- `parentId`: ID of the parent object (required)

**Response (Success):**
```json
{
  "type": "success",
  "message": "Object 12345 attached to parent 67890"
}
```

#### Detach from Parent

Detach an object from its parent:

**Request:**
```json
{
  "type": "hierarchy",
  "command": "detach",
  "childId": 12345
}
```

**Response (Success):**
```json
{
  "type": "success",
  "message": "Object 12345 detached from parent"
}
```

### 🗑️ Delete (Object Deletion)

Delete an object from the scene:

**Request:**
```json
{
  "type": "delete",
  "id": 12345
}
```

**Response (Success):**
```json
{
  "type": "success",
  "message": "Object 12345 deleted successfully"
}
```

**Response (Error):**
```json
{
  "type": "error",
  "message": "Object with ID 12345 not found"
}
```

### 🎥 Camera (Viewport Control)

Control the current viewport/camera view that the user sees through:

#### Set Current View

Set the camera position, rotation, and field of view:

**Request:**
```json
{
  "type": "camera",
  "command": "setview",
  "pos": [0.0, 1.0, 5.0],
  "rot": [10.0, 0.0, 0.0],
  "fov": 35.0
}
```

- `pos`: Camera position [x, y, z] (optional)
- `rot`: Camera rotation [pitch, yaw, roll] in degrees (optional)
- `fov`: Field of view in degrees (optional)

**Response (Success):**
```json
{
  "type": "success",
  "message": "Camera view updated successfully"
}
```

#### Switch to Camera Object

Switch the viewport to a specific camera object:

**Request:**
```json
{
  "type": "camera",
  "command": "switch",
  "cameraId": 12345
}
```

- `cameraId`: ID of the camera object to switch to

**Response (Success):**
```json
{
  "type": "success",
  "message": "Switched to camera 12345"
}
```

#### Switch to Free Camera

Return to free camera mode (default):

**Request:**
```json
{
  "type": "camera",
  "command": "free"
}
```

**Response (Success):**
```json
{
  "type": "success",
  "message": "Switched to free camera mode"
}
```

#### Get Current View

Retrieve current camera information:

**Request:**
```json
{
  "type": "camera",
  "command": "getview"
}
```

**Response (Free Camera Mode):**
```json
{
  "type": "success",
  "message": "Current camera view retrieved",
  "pos": [0.0, 1.0, 5.0],
  "rot": [10.0, 0.0, 0.0],
  "fov": 35.0,
  "mode": "free",
  "activeCameraId": null
}
```

**Response (Camera Object Mode):**
```json
{
  "type": "success",
  "message": "Current camera view retrieved",
  "pos": [0.0, 1.0, 5.0],
  "rot": [10.0, 0.0, 0.0],
  "fov": 35.0,
  "mode": "object",
  "activeCameraId": 12345
}
```

## 💡 Usage Examples

### Web Browser Console

```javascript
// Connect to WebSocket
const ws = new WebSocket('ws://127.0.0.1:8765/ws');

// Send ping
ws.send(JSON.stringify({
  "type": "ping",
  "message": "hello",
  "timestamp": Date.now()
}));

// Get scene structure
ws.send(JSON.stringify({"type": "tree"}));

// Add an item
ws.send(JSON.stringify({
  "type": "add",
  "command": "item",
  "group": 0,
  "category": 0,
  "itemId": 1
}));

// Add a folder
ws.send(JSON.stringify({
  "type": "add",
  "command": "folder",
  "name": "My Objects"
}));

// Move an object
ws.send(JSON.stringify({
  "type": "update",
  "command": "transform",
  "id": 12345,
  "pos": [1.0, 2.0, 3.0]
}));

// Change item color to red
ws.send(JSON.stringify({
  "type": "update",
  "command": "color",
  "id": 12345,
  "color": [1.0, 0.0, 0.0],
  "colorIndex": 0
}));

// Set item transparency to 50%
ws.send(JSON.stringify({
  "type": "update",
  "command": "color",
  "id": 12345,
  "alpha": 0.5
}));

// Attach object to another object
ws.send(JSON.stringify({
  "type": "hierarchy",
  "command": "attach",
  "childId": 12345,
  "parentId": 67890
}));

// Delete an object
ws.send(JSON.stringify({
  "type": "delete",
  "id": 12345
}));

// Add a camera object
ws.send(JSON.stringify({
  "type": "add",
  "command": "camera",
  "name": "Main Camera"
}));

// Set camera view
ws.send(JSON.stringify({
  "type": "camera",
  "command": "setview",
  "pos": [0.0, 2.0, 5.0],
  "rot": [15.0, 0.0, 0.0],
  "fov": 35.0
}));

// Get current camera view
ws.send(JSON.stringify({
  "type": "camera",
  "command": "getview"
}));
```

### Python Example

```python
import websocket
import json

def on_message(ws, message):
    response = json.loads(message)
    print("Received:", response)

def on_open(ws):
    # Test connection
    ws.send(json.dumps({"type": "ping", "message": "hello"}))
    
    # Get scene objects
    ws.send(json.dumps({"type": "tree"}))

ws = websocket.WebSocketApp("ws://127.0.0.1:8765/ws",
                           on_message=on_message,
                           on_open=on_open)
ws.run_forever()
```

## 🔧 Troubleshooting

### Connection Issues

- **Port already in use**: Change the port in configuration settings
- **Cannot connect**: Ensure the game is running and in Studio mode
- **Plugin not loading**: Check BepInEx logs for errors

### Command Issues

- **Object not found**: Use the `tree` command to get valid object IDs
- **Invalid parameters**: Check parameter ranges and data types
- **Character file not found**: Ensure the character file path exists and is accessible

## 🛠️ Development

Want to contribute or build from source? See [CONTRIBUTING.md](CONTRIBUTING.md) for development information.

## 📄 License

This project is licensed under the GNU General Public License v3.0. See [LICENSE](LICENSE) for details.

## ⚠️ Disclaimer

- This plugin is under active development
- Use at your own risk
- Backup your save files before use
- Some features may be experimental