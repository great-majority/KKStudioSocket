using System;
using WebSocketSharp;
using Newtonsoft.Json;

namespace KKStudioSocket.Commands
{
    public class PingCommandHandler : BaseCommandHandler
    {
        public PingCommandHandler(System.Action<string> sendCallback) : base(sendCallback) { }
        
        public void Handle(PingCommand cmd)
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
                KKStudioSocketPlugin.Logger.LogDebug($"Ping received, Pong response: {jsonResponse}");
                
                Send(jsonResponse);
            }
            catch (Exception ex)
            {
                KKStudioSocketPlugin.Logger.LogError($"Ping processing error: {ex.Message}");
            }
        }

        private long GetUnixTimeMilliseconds()
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (long)(DateTime.UtcNow - epoch).TotalMilliseconds;
        }
    }
}