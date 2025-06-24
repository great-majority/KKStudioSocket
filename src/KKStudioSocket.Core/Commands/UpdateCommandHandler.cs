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
                    case "visibility":
                        HandleVisibilityUpdate(cmd);
                        break;
                    case "light":
                        HandleLightUpdate(cmd);
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
                    KKStudioSocketPlugin.Logger.LogDebug($"Transform updated for object ID {cmd.id}: pos={changeAmount.pos}, rot={changeAmount.rot}, scale={changeAmount.scale}");
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

                    KKStudioSocketPlugin.Logger.LogDebug($"Color updated for item ID {cmd.id}: colorIndex={cmd.colorIndex}, color={newColor}");
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

                    KKStudioSocketPlugin.Logger.LogDebug($"Alpha updated for item ID {cmd.id}: alpha={alphaValue}");
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

        private void HandleVisibilityUpdate(UpdateCommand cmd)
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

                if (!cmd.visible.HasValue)
                {
                    KKStudioSocketPlugin.Logger.LogWarning($"Visibility value not provided for object ID {cmd.id}");
                    SendErrorResponse($"Visibility value not provided for object ID {cmd.id}");
                    return;
                }

                bool newVisibility = cmd.visible.Value;

                // Update visibility through TreeNodeObject to ensure UI sync
                try
                {
                    var studio = Studio.Studio.Instance;
                    var targetTreeNode = studio.dicInfo.FirstOrDefault(kvp => kvp.Value == oci).Key;
                    
                    if (targetTreeNode != null)
                    {
                        // Use TreeNodeObject.SetVisible for proper UI sync
                        targetTreeNode.SetVisible(newVisibility);
                        
                        KKStudioSocketPlugin.Logger.LogDebug($"Visibility updated for object ID {cmd.id}: visible={newVisibility}");
                        SendSuccessResponse($"Visibility updated for object ID {cmd.id}");
                    }
                    else
                    {
                        // Fallback: direct visibility update without UI sync
                        oci.OnVisible(newVisibility);
                        
                        KKStudioSocketPlugin.Logger.LogDebug($"Visibility updated (direct) for object ID {cmd.id}: visible={newVisibility}");
                        SendSuccessResponse($"Visibility updated for object ID {cmd.id}");
                    }
                }
                catch (System.Exception ex)
                {
                    KKStudioSocketPlugin.Logger.LogError($"Failed to update visibility with UI sync: {ex.Message}");
                    
                    // Fallback: direct visibility update
                    try
                    {
                        oci.OnVisible(newVisibility);
                        SendSuccessResponse($"Visibility updated for object ID {cmd.id}");
                    }
                    catch
                    {
                        throw; // Re-throw the original exception
                    }
                }
            }
            catch (System.Exception ex)
            {
                KKStudioSocketPlugin.Logger.LogError($"Visibility update error: {ex.Message}");
                SendErrorResponse($"Visibility update error: {ex.Message}");
            }
        }

        private void HandleLightUpdate(UpdateCommand cmd)
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

                // Check if the object is a light
                if (!(oci is Studio.OCILight ociLight))
                {
                    KKStudioSocketPlugin.Logger.LogWarning($"Object with ID {cmd.id} is not a light (type: {oci.GetType().Name})");
                    SendErrorResponse($"Object with ID {cmd.id} is not a light. Light commands can only be used on light objects.");
                    return;
                }

                bool updated = false;


                // Update color if provided
                if (cmd.color != null && cmd.color.Length >= 3)
                {
                    Color newColor = new Color(cmd.color[0], cmd.color[1], cmd.color[2], 1.0f);
                    ociLight.SetColor(newColor);
                    updated = true;
                    KKStudioSocketPlugin.Logger.LogDebug($"Light color updated for ID {cmd.id}: color={newColor}");
                }

                // Update intensity if provided
                if (cmd.intensity.HasValue)
                {
                    float clampedIntensity = UnityEngine.Mathf.Clamp(cmd.intensity.Value, 0.1f, 2.0f);
                    if (ociLight.SetIntensity(clampedIntensity))
                    {
                        updated = true;
                        KKStudioSocketPlugin.Logger.LogDebug($"Light intensity updated for ID {cmd.id}: intensity={clampedIntensity}");
                    }
                }

                // Update range if provided
                if (cmd.range.HasValue)
                {
                    float minRange = (ociLight.lightType == UnityEngine.LightType.Spot) ? 0.5f : 0.1f;
                    float clampedRange = UnityEngine.Mathf.Clamp(cmd.range.Value, minRange, 100.0f);
                    if (ociLight.SetRange(clampedRange))
                    {
                        updated = true;
                        KKStudioSocketPlugin.Logger.LogDebug($"Light range updated for ID {cmd.id}: range={clampedRange}");
                    }
                }

                // Update spot angle if provided (only for spot lights)
                if (cmd.spotAngle.HasValue)
                {
                    if (ociLight.lightType == UnityEngine.LightType.Spot)
                    {
                        float clampedAngle = UnityEngine.Mathf.Clamp(cmd.spotAngle.Value, 1.0f, 179.0f);
                        if (ociLight.SetSpotAngle(clampedAngle))
                        {
                            updated = true;
                            KKStudioSocketPlugin.Logger.LogDebug($"Light spot angle updated for ID {cmd.id}: spotAngle={clampedAngle}");
                        }
                    }
                    else
                    {
                        KKStudioSocketPlugin.Logger.LogWarning($"Spot angle can only be set for spot lights. Light ID {cmd.id} is {ociLight.lightType}");
                    }
                }

                // Update enable state if provided
                if (cmd.enable.HasValue)
                {
                    bool enableValue = cmd.enable.Value;
                    // Force the enable state to ensure it's applied even if it's the same value
                    if (ociLight.SetEnable(enableValue, true)) // _force = true
                    {
                        updated = true;
                        KKStudioSocketPlugin.Logger.LogDebug($"Light enable state updated for ID {cmd.id}: enabled={enableValue}");
                    }
                    else
                    {
                        // Even if SetEnable returns false, consider it updated for UI sync
                        updated = true;
                        KKStudioSocketPlugin.Logger.LogDebug($"Light enable state forced for ID {cmd.id}: enabled={enableValue}");
                    }
                }

                if (updated)
                {
                    // Force UI update to ensure light changes are immediately visible
                    try
                    {
                        // Update UI panel if this light is currently selected
                        var studio = Studio.Studio.Instance;
                        var selectedObjects = studio.treeNodeCtrl.selectObjectCtrl;
                        if (selectedObjects != null && selectedObjects.Contains(oci))
                        {
                            // Find MPLightCtrl component in the scene
                            var mpLightCtrl = UnityEngine.Object.FindObjectOfType<MPLightCtrl>();
                            if (mpLightCtrl != null && mpLightCtrl.ociLight == ociLight)
                            {
                                // Use reflection to set isUpdateInfo flag to prevent toggle event loops
                                var isUpdateInfoField = typeof(MPLightCtrl).GetField("isUpdateInfo", 
                                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                if (isUpdateInfoField != null)
                                {
                                    // Set isUpdateInfo to true to prevent OnValueChangeEnable from triggering
                                    isUpdateInfoField.SetValue(mpLightCtrl, true);
                                    
                                    // Get toggleVisible field and update it directly
                                    var toggleField = typeof(MPLightCtrl).GetField("toggleVisible", 
                                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                                    if (toggleField != null)
                                    {
                                        var toggle = (UnityEngine.UI.Toggle)toggleField.GetValue(mpLightCtrl);
                                        if (toggle != null)
                                        {
                                            // Directly update the toggle state while isUpdateInfo is true
                                            toggle.isOn = ociLight.lightInfo.enable;
                                        }
                                    }
                                    
                                    // Reset isUpdateInfo flag
                                    isUpdateInfoField.SetValue(mpLightCtrl, false);
                                }
                                else
                                {
                                    // Fallback: normal UpdateInfo call
                                    mpLightCtrl.UpdateInfo();
                                }
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        KKStudioSocketPlugin.Logger.LogWarning($"Failed to force light UI update: {ex.Message}");
                    }
                    
                    SendSuccessResponse($"Light properties updated for ID {cmd.id}");
                }
                else
                {
                    KKStudioSocketPlugin.Logger.LogWarning($"No valid light properties provided for ID {cmd.id}");
                    SendErrorResponse($"No valid light properties provided for ID {cmd.id}");
                }
            }
            catch (System.Exception ex)
            {
                KKStudioSocketPlugin.Logger.LogError($"Light update error: {ex.Message}");
                SendErrorResponse($"Light update error: {ex.Message}");
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