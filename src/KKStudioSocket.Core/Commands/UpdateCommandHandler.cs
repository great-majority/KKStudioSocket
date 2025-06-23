using System;
using System.Linq;
using WebSocketSharp;
using UnityEngine;
using Studio;

namespace KKStudioSocket.Commands
{
    public class UpdateCommandHandler : BaseCommandHandler
    {
        public UpdateCommandHandler(System.Action<string> sendCallback) : base(sendCallback) { }
        
        public void Handle(UpdateCommand cmd)
        {
            ApplyUpdate(cmd);
        }

        private void ApplyUpdate(UpdateCommand cmd)
        {
            try
            {
                switch (cmd.command?.ToLower())
                {
                    case "transform":
                        HandleTransformUpdate(cmd);
                        break;
                    case "color":
                        HandleColorUpdate(cmd);
                        break;
                    default:
                        KKStudioSocketPlugin.Logger.LogWarning($"Unsupported update command: {cmd.command}");
                        SendErrorResponse($"Unsupported update command: {cmd.command}");
                        break;
                }
            }
            catch (Exception ex)
            {
                KKStudioSocketPlugin.Logger.LogError($"Update command error: {ex.Message}");
                SendErrorResponse($"Update command error: {ex.Message}");
            }
        }

        private void HandleTransformUpdate(UpdateCommand cmd)
        {
            try
            {

                var oci = Studio.Studio.Instance.dicInfo.Values
                    .FirstOrDefault(info => info.objectInfo.dicKey == cmd.id);

                if (oci == null)
                {
                    KKStudioSocketPlugin.Logger.LogWarning($"Object with ID {cmd.id} not found");
                    SendErrorResponse($"Object with ID {cmd.id} not found");
                    return;
                }

                var changeAmount = oci.objectInfo.changeAmount;
                bool updated = false;

                if (cmd.pos != null && cmd.pos.Length >= 3)
                {
                    changeAmount.pos = new Vector3(cmd.pos[0], cmd.pos[1], cmd.pos[2]);
                    updated = true;
                }

                if (cmd.rot != null && cmd.rot.Length >= 3)
                {
                    changeAmount.rot = new Vector3(cmd.rot[0], cmd.rot[1], cmd.rot[2]);
                    updated = true;
                }

                if (cmd.scale != null && cmd.scale.Length >= 3)
                {
                    changeAmount.scale = new Vector3(cmd.scale[0], cmd.scale[1], cmd.scale[2]);
                    updated = true;
                }

                if (updated)
                {
                    KKStudioSocketPlugin.Logger.LogInfo($"Transform updated for object ID {cmd.id}: pos={changeAmount.pos}, rot={changeAmount.rot}, scale={changeAmount.scale}");
                    SendSuccessResponse($"Transform updated for object ID {cmd.id}");
                }
                else
                {
                    KKStudioSocketPlugin.Logger.LogWarning($"No valid transform data provided for object ID {cmd.id}");
                    SendErrorResponse($"No valid transform data provided for object ID {cmd.id}");
                }
            }
            catch (Exception ex)
            {
                KKStudioSocketPlugin.Logger.LogError($"Update command error: {ex.Message}");
                SendErrorResponse($"Update command error: {ex.Message}");
            }
        }

        private void HandleColorUpdate(UpdateCommand cmd)
        {
            try
            {
                var oci = Studio.Studio.Instance.dicInfo.Values
                    .FirstOrDefault(info => info.objectInfo.dicKey == cmd.id);

                if (oci == null)
                {
                    KKStudioSocketPlugin.Logger.LogWarning($"Object with ID {cmd.id} not found");
                    SendErrorResponse($"Object with ID {cmd.id} not found");
                    return;
                }

                // Check if the object is an item
                if (!(oci is Studio.OCIItem ociItem))
                {
                    KKStudioSocketPlugin.Logger.LogWarning($"Object with ID {cmd.id} is not an item (type: {oci.GetType().Name})");
                    SendErrorResponse($"Object with ID {cmd.id} is not an item. Color can only be changed for items.");
                    return;
                }

                bool updated = false;

                // Update color if provided
                if (cmd.color != null && cmd.color.Length >= 3)
                {
                    // Color index validation (0-7)
                    if (cmd.colorIndex < 0 || cmd.colorIndex > 7)
                    {
                        KKStudioSocketPlugin.Logger.LogWarning($"Invalid color index: {cmd.colorIndex}. Must be 0-7.");
                        SendErrorResponse($"Invalid color index: {cmd.colorIndex}. Must be 0-7.");
                        return;
                    }

                    // Create color from RGB values (with optional alpha)
                    Color newColor = cmd.color.Length >= 4 
                        ? new Color(cmd.color[0], cmd.color[1], cmd.color[2], cmd.color[3])
                        : new Color(cmd.color[0], cmd.color[1], cmd.color[2], 1.0f);

                    ociItem.SetColor(newColor, cmd.colorIndex);
                    updated = true;

                    KKStudioSocketPlugin.Logger.LogInfo($"Color updated for item ID {cmd.id}: colorIndex={cmd.colorIndex}, color={newColor}");
                }

                // Update alpha if provided (for overall transparency)
                if (cmd.alpha.HasValue)
                {
                    float alphaValue = cmd.alpha.Value;
                    if (alphaValue > 1.0f)
                    {
                        KKStudioSocketPlugin.Logger.LogWarning($"Alpha value {alphaValue} clamped to 1.0");
                        alphaValue = 1.0f;
                    }

                    ociItem.SetAlpha(alphaValue);
                    updated = true;

                    KKStudioSocketPlugin.Logger.LogInfo($"Alpha updated for item ID {cmd.id}: alpha={alphaValue}");
                }

                if (updated)
                {
                    // Force UI update to ensure color changes are immediately visible
                    try
                    {
                        ociItem.UpdateColor();
                        
                        // Update UI panel if this object is currently selected
                        var studio = Studio.Studio.Instance;
                        var selectedObjects = studio.treeNodeCtrl.selectObjectCtrl;
                        if (selectedObjects != null && selectedObjects.Contains(ociItem))
                        {
                            // Object is currently selected, update the UI panel
                            // Find MPItemCtrl component in the scene
                            var mpItemCtrl = UnityEngine.Object.FindObjectOfType<MPItemCtrl>();
                            if (mpItemCtrl != null && mpItemCtrl.ociItem == ociItem)
                            {
                                mpItemCtrl.UpdateInfo();
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        KKStudioSocketPlugin.Logger.LogWarning($"Failed to force color update: {ex.Message}");
                    }
                    
                    SendSuccessResponse($"Color updated for item ID {cmd.id}");
                }
                else
                {
                    KKStudioSocketPlugin.Logger.LogWarning($"No valid color data provided for item ID {cmd.id}");
                    SendErrorResponse($"No valid color data provided for item ID {cmd.id}");
                }
            }
            catch (Exception ex)
            {
                KKStudioSocketPlugin.Logger.LogError($"Color update error: {ex.Message}");
                SendErrorResponse($"Color update error: {ex.Message}");
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