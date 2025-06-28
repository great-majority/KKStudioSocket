using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;
using Newtonsoft.Json;
using KKAPI;
using Studio;
using System.Linq;
using KKStudioSocket.Commands;

namespace KKStudioSocket
{
    [BepInPlugin(GUID, Name, Version)]
    public class KKStudioSocketPlugin : BaseUnityPlugin
    {
        public const string GUID = "com.github.kk.kkstudiosocket";
        public const string Name = "KKStudioSocket";
        public const string Version = "1.0.0";

        private const int DefaultPort = 8765;
        private const string DefaultPath = "/ws";

        internal static new ManualLogSource Logger;
        private static readonly Queue<Action> _actionQueue = new Queue<Action>();

        private ConfigEntry<int> _serverPort;
        private ConfigEntry<bool> _enableServer;
        private WebSocketServer _webSocketServer;
        private Studio.Studio _studio = null;

        private void Awake()
        {
            Logger = base.Logger;
            Logger.LogInfo($"Awaking {Name} v{Version}");
        }

        private void Update()
        {
            if (KKAPI.KoikatuAPI.GetCurrentGameMode() == GameMode.Studio)
            {
                if (this._studio == null && Studio.Studio.instance != null)
                {
                    this._studio = Studio.Studio.instance;
                    Logger.LogInfo("Studio instance seems to be initialized. Starting WebSocket Server.");

                    _serverPort = Config.Bind("Server", "Port", DefaultPort, "WebSocket server port number");
                    _enableServer = Config.Bind("Server", "Enable", true, "Enable WebSocket server");

                    if (_enableServer.Value)
                    {
                        StartWebSocketServer();
                    }
                }

                lock (_actionQueue)
                {
                    while (_actionQueue.Count > 0)
                        _actionQueue.Dequeue().Invoke();
                }
            }
        }

        private void StartWebSocketServer()
        {
            try
            {
                _webSocketServer = new WebSocketServer(System.Net.IPAddress.Loopback, _serverPort.Value);
                _webSocketServer.AddWebSocketService<StudioWsBehavior>(DefaultPath);
                _webSocketServer.Start();
                Logger.LogInfo($"WebSocket server started → ws://127.0.0.1:{_serverPort.Value}{DefaultPath}");
            }
            catch (Exception e)
            {
                Logger.LogFatal($"Failed to start WebSocket server: {e.Message}");
                Logger.LogFatal("KKStudioSocket plugin will be disabled due to critical startup failure.");
                this.enabled = false;
            }
        }

        private void OnDestroy()
        {
            if (KKAPI.KoikatuAPI.GetCurrentGameMode() == GameMode.Studio && this._studio != null)
            {
                _webSocketServer?.Stop();
                Logger.LogInfo("WebSocket server stopped");
            }
        }

        internal static void EnqueueMainThreadAction(Action action)
        {
            lock (_actionQueue)
            {
                _actionQueue.Enqueue(action);
            }
        }

    }

    internal class StudioWsBehavior : WebSocketBehavior
    {
        private void EnqueueAction(Action action)
        {
            KKStudioSocketPlugin.EnqueueMainThreadAction(action);
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            try
            {
                KKStudioSocketPlugin.Logger.LogDebug($"Received WebSocket message: {e.Data}");

                // Determine command type
                var baseCommand = JsonConvert.DeserializeObject<BaseCommand>(e.Data);
                if (baseCommand?.type == null)
                {
                    KKStudioSocketPlugin.Logger.LogWarning($"Command type not specified: {e.Data}");
                    return;
                }

                KKStudioSocketPlugin.Logger.LogDebug($"Processing command type: {baseCommand.type}");

                switch (baseCommand.type.ToLower())
                {
                    case "ping":
                        KKStudioSocketPlugin.Logger.LogDebug("Handling ping command");
                        var pingCmd = JsonConvert.DeserializeObject<PingCommand>(e.Data);
                        if (pingCmd != null)
                        {
                            KKStudioSocketPlugin.Logger.LogDebug($"Ping command parsed: message={pingCmd.message}");
                            var pingHandler = new PingCommandHandler(Send);
                            pingHandler.Handle(pingCmd);
                            KKStudioSocketPlugin.Logger.LogDebug("Ping handler executed");
                        }
                        else
                        {
                            KKStudioSocketPlugin.Logger.LogWarning("Failed to parse ping command");
                        }
                        break;

                    case "tree":
                        KKStudioSocketPlugin.Logger.LogDebug("Handling tree command");
                        var treeCmd = JsonConvert.DeserializeObject<TreeCommand>(e.Data);
                        if (treeCmd != null)
                        {
                            var treeHandler = new TreeCommandHandler(Send);
                            treeHandler.Handle(treeCmd);
                        }
                        else
                        {
                            // Fallback for backward compatibility
                            var treeHandler = new TreeCommandHandler(Send);
                            treeHandler.Handle();
                        }
                        KKStudioSocketPlugin.Logger.LogDebug("Tree handler executed");
                        break;

                    case "item":
                        KKStudioSocketPlugin.Logger.LogDebug("Handling item command");
                        var itemCmd = JsonConvert.DeserializeObject<ItemCommand>(e.Data);
                        if (itemCmd != null)
                        {
                            var itemHandler = new ItemCommandHandler(Send);
                            itemHandler.Handle(itemCmd);
                        }
                        else
                        {
                            KKStudioSocketPlugin.Logger.LogWarning("Failed to parse item command");
                        }
                        KKStudioSocketPlugin.Logger.LogDebug("Item handler executed");
                        break;

                    case "update":
                        var updateCmd = JsonConvert.DeserializeObject<UpdateCommand>(e.Data);
                        if (updateCmd != null)
                        {
                            EnqueueAction(() =>
                            {
                                var updateHandler = new UpdateCommandHandler(Send);
                                updateHandler.Handle(updateCmd);
                            });
                        }
                        break;

                    case "add":
                        var addCmd = JsonConvert.DeserializeObject<AddCommand>(e.Data);
                        if (addCmd != null)
                        {
                            EnqueueAction(() =>
                            {
                                var addHandler = new AddCommandHandler(Send);
                                addHandler.Handle(addCmd);
                            });
                        }
                        break;

                    case "hierarchy":
                        var hierarchyCmd = JsonConvert.DeserializeObject<HierarchyCommand>(e.Data);
                        if (hierarchyCmd != null)
                        {
                            EnqueueAction(() =>
                            {
                                var hierarchyHandler = new HierarchyCommandHandler(Send);
                                hierarchyHandler.Handle(hierarchyCmd);
                            });
                        }
                        break;

                    case "delete":
                        var deleteCmd = JsonConvert.DeserializeObject<DeleteCommand>(e.Data);
                        if (deleteCmd != null)
                        {
                            EnqueueAction(() =>
                            {
                                var deleteHandler = new DeleteCommandHandler(Send);
                                deleteHandler.Handle(deleteCmd);
                            });
                        }
                        break;

                    case "camera":
                        var cameraCmd = JsonConvert.DeserializeObject<CameraCommand>(e.Data);
                        if (cameraCmd != null)
                        {
                            EnqueueAction(() =>
                            {
                                var cameraHandler = new CameraCommandHandler(Send);
                                cameraHandler.Handle(cameraCmd);
                            });
                        }
                        break;


                    default:
                        KKStudioSocketPlugin.Logger.LogWarning($"Unsupported command type: {baseCommand.type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                KKStudioSocketPlugin.Logger.LogError($"WebSocket message processing error: {ex.Message}");
                KKStudioSocketPlugin.Logger.LogDebug($"Received data: {e.Data}");
            }
        }


        protected override void OnOpen()
        {
            KKStudioSocketPlugin.Logger.LogInfo("Client connected");
        }

        protected override void OnClose(CloseEventArgs e)
        {
            KKStudioSocketPlugin.Logger.LogInfo($"Client disconnected (Code: {e.Code}, Reason: {e.Reason})");
        }

        protected override void OnError(ErrorEventArgs e)
        {
            KKStudioSocketPlugin.Logger.LogError($"WebSocket error: {e.Message}");
        }
    }

    [Serializable]
    public class BaseCommand
    {
        public string type;
    }

    [Serializable]
    public class PingCommand : BaseCommand
    {
        public string message;
        public long timestamp;
    }

    [Serializable]
    public class PongResponse
    {
        public string type;
        public string message;
        public long timestamp;
    }

    [Serializable]
    public class UpdateCommand : BaseCommand
    {
        public string command;
        public int id;
        public float[] pos;
        public float[] rot;
        public float[] scale;
        public float[] color;
        public int colorIndex;
        public float? alpha;
        public bool? visible;
        public float? intensity; // Light intensity (0.1-2.0)
        public float? range; // Light range (Point: 0.1-100, Spot: 0.5-100)
        public float? spotAngle; // Spot angle (1-179 degrees)
        public bool? enable; // Light enabled/disabled
    }

    [Serializable]
    public class AddCommand : BaseCommand
    {
        public string command;
        public int group;
        public int category;
        public int itemId;
        public int lightId;
        public int? parentId;
        public string path;
        public string sex;
        public string name;
    }

    [Serializable]
    public class HierarchyCommand : BaseCommand
    {
        public string command;
        public int childId;
        public int parentId; // Required for attach, ignored for detach
    }

    [Serializable]
    public class DeleteCommand : BaseCommand
    {
        public int id;
    }

    [Serializable]
    public class CameraCommand : BaseCommand
    {
        public string command;
        public float[] pos;
        public float[] rot;
        public float fov;
        public int cameraId; // For switching to specific camera object
    }

    [Serializable]
    public class ItemCommand : BaseCommand
    {
        public string command;
        public int groupId = -1;
        public int categoryId = -1;
    }

    [Serializable]
    public class TreeCommand : BaseCommand
    {
        public int? depth; // Maximum depth to retrieve (null = unlimited)
        public int? id; // Specific object ID to start from (null = all roots)
    }

}