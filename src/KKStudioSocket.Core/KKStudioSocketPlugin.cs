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
using KKStudioSocket.Models.Requests;
using KKStudioSocket.Models.Responses;

namespace KKStudioSocket
{
    [BepInPlugin(GUID, Name, Version)]
    public class KKStudioSocketPlugin : BaseUnityPlugin
    {
        public const string GUID = "KKStudioSocket";
        public const string Name = "KKStudioSocket";
        public const string Version = "1.0.0";

        private const int DefaultPort = 8765;
        private const string DefaultPath = "/ws";
        private const string DefaultBindAddress = "127.0.0.1";

        internal static new ManualLogSource Logger;
        private static readonly Queue<Action> _actionQueue = new Queue<Action>();

        private ConfigEntry<int> _serverPort;
        private ConfigEntry<string> _serverBindAddress;
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

                    _serverBindAddress = Config.Bind("Server", "BindAddress", DefaultBindAddress, "IP address for the WebSocket server to bind to");
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
                _webSocketServer = new WebSocketServer($"ws://{_serverBindAddress.Value}:{_serverPort.Value}");
                _webSocketServer.AddWebSocketService<StudioWsBehavior>(DefaultPath);
                _webSocketServer.Start();
                Logger.LogInfo($"WebSocket server started → ws://{_serverBindAddress.Value}:{_serverPort.Value}{DefaultPath}");
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
                        var itemCmd = JsonConvert.DeserializeObject<Models.Requests.ItemCommand>(e.Data);
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

                    case "screenshot":
                        var screenshotCmd = JsonConvert.DeserializeObject<ScreenshotCommand>(e.Data);
                        if (screenshotCmd != null)
                        {
                            EnqueueAction(() =>
                            {
                                var screenshotHandler = new ScreenshotCommandHandler(Send);
                                screenshotHandler.Handle(screenshotCmd);
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

}