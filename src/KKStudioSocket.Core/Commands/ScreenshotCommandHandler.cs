using System;
using Newtonsoft.Json;
using Studio;
using KKStudioSocket.Models.Requests;
using KKStudioSocket.Models.Responses;

namespace KKStudioSocket.Commands
{
    public class ScreenshotCommandHandler : BaseCommandHandler
    {
        public ScreenshotCommandHandler(System.Action<string> sendCallback) : base(sendCallback) { }

        public void Handle(ScreenshotCommand cmd)
        {
            try
            {
                // Default to 480p (854x480) if not specified
                int width = cmd.width ?? 854;
                int height = cmd.height ?? 480;
                bool transparency = cmd.transparency ?? false;
                bool mark = cmd.mark ?? true;

                KKStudioSocketPlugin.Logger.LogDebug($"Screenshot command: {width}x{height}, transparency={transparency}, mark={mark}");

                // Get screenshot from Studio
                if (Studio.Studio.Instance?.gameScreenShot != null)
                {
                    byte[] pngData = Studio.Studio.Instance.gameScreenShot.CreatePngScreen(width, height, transparency, mark);
                    
                    if (pngData != null && pngData.Length > 0)
                    {
                        // Convert to Base64
                        string base64Image = Convert.ToBase64String(pngData);
                        
                        var response = new ScreenshotSuccessResponse
                        {
                            message = "Screenshot captured successfully",
                            data = new ScreenshotData
                            {
                                image = base64Image,
                                width = width,
                                height = height,
                                format = "png",
                                transparency = transparency,
                                size = pngData.Length
                            }
                        };

                        var jsonResponse = JsonConvert.SerializeObject(response);
                        KKStudioSocketPlugin.Logger.LogInfo($"Screenshot captured: {width}x{height}, size: {pngData.Length} bytes");
                        Send(jsonResponse);
                    }
                    else
                    {
                        SendErrorResponse("Failed to generate screenshot - no data returned");
                    }
                }
                else
                {
                    SendErrorResponse("GameScreenShot instance not available");
                }
            }
            catch (Exception ex)
            {
                KKStudioSocketPlugin.Logger.LogError($"Screenshot command error: {ex.Message}");
                SendErrorResponse($"Screenshot failed: {ex.Message}");
            }
        }

        private void SendErrorResponse(string message)
        {
            var errorResponse = new ErrorResponse(message);
            Send(JsonConvert.SerializeObject(errorResponse));
        }
    }
}