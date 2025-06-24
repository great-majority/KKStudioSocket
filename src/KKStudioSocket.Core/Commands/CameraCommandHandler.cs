using System;
using System.Linq;
using UnityEngine;
using WebSocketSharp;
using Studio;

namespace KKStudioSocket.Commands
{
    public class CameraCommandHandler : BaseCommandHandler
    {
        public CameraCommandHandler(System.Action<string> sendCallback) : base(sendCallback) { }
        
        public void Handle(CameraCommand cmd)
        {
            ApplyCamera(cmd);
        }

        private void ApplyCamera(CameraCommand cmd)
        {
            try
            {
                switch (cmd.command?.ToLower())
                {
                    case "setview":
                        HandleSetView(cmd);
                        break;
                    case "switch":
                        HandleSwitchCamera(cmd);
                        break;
                    case "getview":
                        HandleGetView(cmd);
                        break;
                    case "free":
                        HandleFreeCamera(cmd);
                        break;
                    default:
                        KKStudioSocketPlugin.Logger.LogWarning($"Unsupported camera command: {cmd.command}");
                        SendErrorResponse($"Unsupported camera command: {cmd.command}");
                        break;
                }
            }
            catch (Exception ex)
            {
                KKStudioSocketPlugin.Logger.LogError($"Camera command error: {ex.Message}");
                SendErrorResponse($"Camera command error: {ex.Message}");
            }
        }

        private void HandleSetView(CameraCommand cmd)
        {
            try
            {
                var studio = Studio.Studio.Instance;
                var cameraCtrl = studio.cameraCtrl;

                // Set camera position
                if (cmd.pos != null && cmd.pos.Length >= 3)
                {
                    cameraCtrl.targetPos = new Vector3(cmd.pos[0], cmd.pos[1], cmd.pos[2]);
                }

                // Set camera rotation
                if (cmd.rot != null && cmd.rot.Length >= 3)
                {
                    cameraCtrl.cameraAngle = new Vector3(cmd.rot[0], cmd.rot[1], cmd.rot[2]);
                }

                // Set field of view
                if (cmd.fov > 0)
                {
                    cameraCtrl.fieldOfView = cmd.fov;
                }

                KKStudioSocketPlugin.Logger.LogDebug($"Camera view updated: pos={cameraCtrl.targetPos}, rot={cameraCtrl.cameraAngle}, fov={cameraCtrl.fieldOfView}");
                SendSuccessResponse($"Camera view updated successfully");
            }
            catch (Exception ex)
            {
                KKStudioSocketPlugin.Logger.LogError($"Set view error: {ex.Message}");
                SendErrorResponse($"Set view error: {ex.Message}");
            }
        }

        private void HandleSwitchCamera(CameraCommand cmd)
        {
            try
            {
                if (cmd.cameraId < 0)
                {
                    KKStudioSocketPlugin.Logger.LogWarning($"Invalid camera ID: {cmd.cameraId}");
                    SendErrorResponse($"Invalid camera ID: {cmd.cameraId}");
                    return;
                }

                // Find camera object
                var oci = Studio.Studio.Instance.dicInfo.Values
                    .FirstOrDefault(info => info is Studio.OCICamera && info.objectInfo.dicKey == cmd.cameraId) as Studio.OCICamera;

                if (oci == null)
                {
                    KKStudioSocketPlugin.Logger.LogWarning($"Camera object with ID {cmd.cameraId} not found");
                    SendErrorResponse($"Camera object with ID {cmd.cameraId} not found");
                    return;
                }

                // Switch to this camera (this updates UI automatically)
                Studio.Studio.Instance.ChangeCamera(oci);

                KKStudioSocketPlugin.Logger.LogDebug($"Switched to camera {cmd.cameraId}");
                SendSuccessResponse($"Switched to camera {cmd.cameraId}");
            }
            catch (Exception ex)
            {
                KKStudioSocketPlugin.Logger.LogError($"Switch camera error: {ex.Message}");
                SendErrorResponse($"Switch camera error: {ex.Message}");
            }
        }

        private void HandleGetView(CameraCommand cmd)
        {
            try
            {
                var studio = Studio.Studio.Instance;
                var cameraCtrl = studio.cameraCtrl;

                // Check if any camera object is currently active
                var activeCamera = studio.dicInfo.Values
                    .OfType<Studio.OCICamera>()
                    .FirstOrDefault(cam => cam.cameraInfo.active);

                var response = new 
                { 
                    type = "success", 
                    message = "Current camera view retrieved",
                    pos = new float[] { cameraCtrl.targetPos.x, cameraCtrl.targetPos.y, cameraCtrl.targetPos.z },
                    rot = new float[] { cameraCtrl.cameraAngle.x, cameraCtrl.cameraAngle.y, cameraCtrl.cameraAngle.z },
                    fov = cameraCtrl.fieldOfView,
                    mode = activeCamera != null ? "object" : "free",
                    activeCameraId = activeCamera?.objectInfo.dicKey
                };

                Send(Newtonsoft.Json.JsonConvert.SerializeObject(response));
                
                string modeInfo = activeCamera != null 
                    ? $"Camera Object Mode (ID: {activeCamera.objectInfo.dicKey})" 
                    : "Free Camera Mode";
                KKStudioSocketPlugin.Logger.LogDebug($"Camera view retrieved: {modeInfo}, pos={cameraCtrl.targetPos}, rot={cameraCtrl.cameraAngle}, fov={cameraCtrl.fieldOfView}");
            }
            catch (Exception ex)
            {
                KKStudioSocketPlugin.Logger.LogError($"Get view error: {ex.Message}");
                SendErrorResponse($"Get view error: {ex.Message}");
            }
        }

        private void HandleFreeCamera(CameraCommand cmd)
        {
            try
            {
                var studio = Studio.Studio.Instance;

                // Use Studio's method to properly switch to free camera (null camera)
                // This will handle all UI updates automatically
                studio.ChangeCamera(null);

                KKStudioSocketPlugin.Logger.LogDebug("Switched to free camera mode");
                SendSuccessResponse("Switched to free camera mode");
            }
            catch (Exception ex)
            {
                KKStudioSocketPlugin.Logger.LogError($"Free camera error: {ex.Message}");
                SendErrorResponse($"Free camera error: {ex.Message}");
            }
        }
        
        private void SendSuccessResponse(string message)
        {
            var response = new { type = "success", message = message };
            Send(Newtonsoft.Json.JsonConvert.SerializeObject(response));
        }
        
        private void SendErrorResponse(string message)
        {
            var response = new { type = "error", message = message };
            Send(Newtonsoft.Json.JsonConvert.SerializeObject(response));
        }
    }
}