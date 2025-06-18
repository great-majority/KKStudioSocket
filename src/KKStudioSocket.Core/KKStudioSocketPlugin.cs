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

        private void Awake()
        {
            Logger = base.Logger;
            Logger.LogInfo($"Loading {Name} v{Version}");
            
            _serverPort = Config.Bind("Server", "Port", DefaultPort, "WebSocket server port number");
            _enableServer = Config.Bind("Server", "Enable", true, "Enable WebSocket server");
            
            if (_enableServer.Value)
            {
                StartWebSocketServer();
            }
        }

        private void Update()
        {
            lock (_actionQueue)
            {
                while (_actionQueue.Count > 0)
                    _actionQueue.Dequeue().Invoke();
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
                Logger.LogError($"Failed to start WebSocket server: {e.Message}");
            }
        }

        private void OnDestroy()
        {
            _webSocketServer?.Stop();
            Logger.LogInfo("WebSocket server stopped");
        }

        internal static void EnqueueTransform(TransformCommand cmd)
        {
            lock (_actionQueue)
            {
                _actionQueue.Enqueue(() => ApplyTransform(cmd));
            }
        }

        private static void ApplyTransform(TransformCommand cmd)
        {
            try
            {
                var posStr = cmd.pos != null && cmd.pos.Length >= 3 ? $"[{cmd.pos[0]:F2}, {cmd.pos[1]:F2}, {cmd.pos[2]:F2}]" : "null";
                var rotStr = cmd.rot != null && cmd.rot.Length >= 3 ? $"[{cmd.rot[0]:F2}, {cmd.rot[1]:F2}, {cmd.rot[2]:F2}]" : "null";
                
                Logger.LogInfo($"Transform command received: ID={cmd.id}, pos={posStr}, rot={rotStr}");
                
                // TODO: Implement actual object manipulation using Studio API
                // Example: var oci = StudioAPI.GetObject(cmd.id) as OCIItem;
                // Currently only logs output
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }
    }

    internal class StudioWsBehavior : WebSocketBehavior
    {
        protected override void OnMessage(MessageEventArgs e)
        {
            try
            {
                // Determine command type
                var baseCommand = JsonConvert.DeserializeObject<BaseCommand>(e.Data);
                if (baseCommand?.type == null)
                {
                    KKStudioSocketPlugin.Logger.LogWarning($"Command type not specified: {e.Data}");
                    return;
                }

                switch (baseCommand.type.ToLower())
                {
                    case "ping":
                        var pingCmd = JsonConvert.DeserializeObject<PingCommand>(e.Data);
                        if (pingCmd != null)
                        {
                            HandlePingCommand(pingCmd);
                        }
                        break;
                        
                    case "transform":
                        var transformCmd = JsonConvert.DeserializeObject<TransformCommand>(e.Data);
                        if (transformCmd != null)
                        {
                            KKStudioSocketPlugin.EnqueueTransform(transformCmd);
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

        private void HandlePingCommand(PingCommand cmd)
        {
            try
            {
                var pongResponse = new PongResponse
                {
                    type = "pong",
                    timestamp = GetUnixTimeMilliseconds(),
                    message = cmd.message ?? "pong"
                };
                
                var jsonResponse = JsonConvert.SerializeObject(pongResponse);
                Send(jsonResponse);
                
                KKStudioSocketPlugin.Logger.LogInfo($"Ping received, Pong response: {cmd.message}");
            }
            catch (Exception ex)
            {
                KKStudioSocketPlugin.Logger.LogError($"Ping processing error: {ex.Message}");
            }
        }

        private static long GetUnixTimeMilliseconds()
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (long)(DateTime.UtcNow - epoch).TotalMilliseconds;
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
    public class TransformCommand : BaseCommand
    {
        public int id;
        public float[] pos;
        public float[] rot;
    }
}