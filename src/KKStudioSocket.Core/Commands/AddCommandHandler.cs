using System;
using System.Linq;
using WebSocketSharp;
using Studio;

namespace KKStudioSocket.Commands
{
    public class AddCommandHandler : BaseCommandHandler
    {
        public AddCommandHandler(System.Action<string> sendCallback) : base(sendCallback) { }

        public void Handle(AddCommand cmd)
        {
            ApplyAdd(cmd);
        }

        private void ApplyAdd(AddCommand cmd)
        {
            try
            {
                switch (cmd.command?.ToLower())
                {
                    case "item":
                        HandleAddItem(cmd);
                        break;
                    case "light":
                        HandleAddLight(cmd);
                        break;
                    case "character":
                        HandleAddCharacter(cmd);
                        break;
                    case "folder":
                        HandleAddFolder(cmd);
                        break;
                    case "camera":
                        HandleAddCamera(cmd);
                        break;
                    default:
                        KKStudioSocketPlugin.Logger.LogWarning($"Unsupported add command: {cmd.command}");
                        SendErrorResponse($"Unsupported add command: {cmd.command}");
                        break;
                }
            }
            catch (Exception ex)
            {
                KKStudioSocketPlugin.Logger.LogError($"Add command error: {ex.Message}");
            }
        }

        private void HandleAddItem(AddCommand cmd)
        {
            try
            {
                // Parameter validation
                if (cmd.group < 0 || cmd.category < 0 || cmd.itemId < 0)
                {
                    KKStudioSocketPlugin.Logger.LogWarning($"Invalid item parameters: group={cmd.group}, category={cmd.category}, itemId={cmd.itemId}");
                    SendErrorResponse($"Invalid item parameters: group={cmd.group}, category={cmd.category}, itemId={cmd.itemId}");
                    return;
                }

                // Call AddObjectItem directly to get the created object
                var newItem = Studio.AddObjectItem.Add(cmd.group, cmd.category, cmd.itemId);
                int objectId = newItem.objectInfo.dicKey;

                KKStudioSocketPlugin.Logger.LogDebug($"Item added successfully: group={cmd.group}, category={cmd.category}, itemId={cmd.itemId}, objectId={objectId}");
                SendSuccessResponseWithId($"Item added successfully: group={cmd.group}, category={cmd.category}, itemId={cmd.itemId}", objectId);
            }
            catch (Exception ex)
            {
                KKStudioSocketPlugin.Logger.LogError($"Add item error: {ex.Message}");
                SendErrorResponse($"Add item error: {ex.Message}");
            }
        }

        private void HandleAddLight(AddCommand cmd)
        {
            try
            {
                // Parameter validation
                if (cmd.lightId < 0)
                {
                    KKStudioSocketPlugin.Logger.LogWarning($"Invalid light parameter: lightId={cmd.lightId}");
                    SendErrorResponse($"Invalid light parameter: lightId={cmd.lightId}");
                    return;
                }

                // Check light count limit (pre-check in addition to internal Studio check)
                if (!Studio.Studio.Instance.sceneInfo.isLightCheck)
                {
                    KKStudioSocketPlugin.Logger.LogWarning("Light limit reached or light check disabled");
                    SendErrorResponse("Light limit reached or light check disabled");
                    return;
                }

                // Call AddObjectLight directly to get the created object
                var newLight = Studio.AddObjectLight.Add(cmd.lightId);
                int objectId = newLight.objectInfo.dicKey;

                KKStudioSocketPlugin.Logger.LogDebug($"Light added successfully: lightId={cmd.lightId}, objectId={objectId}");
                SendSuccessResponseWithId($"Light added successfully: lightId={cmd.lightId}", objectId);
            }
            catch (Exception ex)
            {
                KKStudioSocketPlugin.Logger.LogError($"Add light error: {ex.Message}");
                SendErrorResponse($"Add light error: {ex.Message}");
            }
        }

        private void HandleAddCharacter(AddCommand cmd)
        {
            try
            {
                // Parameter validation
                if (string.IsNullOrEmpty(cmd.path))
                {
                    KKStudioSocketPlugin.Logger.LogWarning("Character path is required");
                    SendErrorResponse("Character path is required");
                    return;
                }

                if (string.IsNullOrEmpty(cmd.sex))
                {
                    KKStudioSocketPlugin.Logger.LogWarning("Character sex is required (female or male)");
                    SendErrorResponse("Character sex is required (female or male)");
                    return;
                }

                // Check if path exists
                if (!System.IO.File.Exists(cmd.path))
                {
                    KKStudioSocketPlugin.Logger.LogWarning($"Character file not found: {cmd.path}");
                    SendErrorResponse($"Character file not found: {cmd.path}");
                    return;
                }

                // Add character based on sex
                switch (cmd.sex.ToLower())
                {
                    case "female":
                        var newFemale = Studio.AddObjectFemale.Add(cmd.path);
                        int femaleId = newFemale.objectInfo.dicKey;
                        KKStudioSocketPlugin.Logger.LogDebug($"Female character added successfully: {cmd.path}, objectId={femaleId}");
                        SendSuccessResponseWithId($"Female character added successfully: {cmd.path}", femaleId);
                        break;
                    case "male":
                        var newMale = Studio.AddObjectMale.Add(cmd.path);
                        int maleId = newMale.objectInfo.dicKey;
                        KKStudioSocketPlugin.Logger.LogDebug($"Male character added successfully: {cmd.path}, objectId={maleId}");
                        SendSuccessResponseWithId($"Male character added successfully: {cmd.path}", maleId);
                        break;
                    default:
                        KKStudioSocketPlugin.Logger.LogWarning($"Invalid character sex: {cmd.sex} (must be 'female' or 'male')");
                        SendErrorResponse($"Invalid character sex: {cmd.sex} (must be 'female' or 'male')");
                        break;
                }
            }
            catch (Exception ex)
            {
                KKStudioSocketPlugin.Logger.LogError($"Add character error: {ex.Message}");
                SendErrorResponse($"Add character error: {ex.Message}");
            }
        }

        private void HandleAddFolder(AddCommand cmd)
        {
            try
            {
                // Call AddObjectFolder directly to get the created object
                var newFolder = Studio.AddObjectFolder.Add();
                int objectId = newFolder.objectInfo.dicKey;

                // If name is specified, rename the folder
                if (!string.IsNullOrEmpty(cmd.name))
                {
                    newFolder.name = cmd.name;
                    KKStudioSocketPlugin.Logger.LogDebug($"Folder added and renamed to: {cmd.name}, objectId={objectId}");
                    SendSuccessResponseWithId($"Folder added successfully with name: {cmd.name}", objectId);
                }
                else
                {
                    KKStudioSocketPlugin.Logger.LogDebug($"Folder added successfully, objectId={objectId}");
                    SendSuccessResponseWithId("Folder added successfully", objectId);
                }
            }
            catch (Exception ex)
            {
                KKStudioSocketPlugin.Logger.LogError($"Add folder error: {ex.Message}");
                SendErrorResponse($"Add folder error: {ex.Message}");
            }
        }

        private void HandleAddCamera(AddCommand cmd)
        {
            try
            {
                // Call AddObjectCamera directly to get the created object
                var newCamera = Studio.AddObjectCamera.Add();
                int objectId = newCamera.objectInfo.dicKey;

                // Update UI components like Studio.AddCamera() does
                var studio = Studio.Studio.Instance;
                studio.cameraSelector.Init(); // Update camera dropdown
                
                // Auto-select if option is enabled (like Studio.AddCamera())
                if (Studio.Studio.optionSystem.autoSelect && newCamera != null)
                {
                    studio.m_TreeNodeCtrl.SelectSingle(newCamera.treeNodeObject);
                }

                // If name is specified, rename the camera
                if (!string.IsNullOrEmpty(cmd.name))
                {
                    newCamera.name = cmd.name;
                    // Update UI again after name change
                    studio.cameraSelector.Init();
                    KKStudioSocketPlugin.Logger.LogDebug($"Camera added and renamed to: {cmd.name}, objectId={objectId}");
                    SendSuccessResponseWithId($"Camera added successfully with name: {cmd.name}", objectId);
                }
                else
                {
                    KKStudioSocketPlugin.Logger.LogDebug($"Camera added successfully, objectId={objectId}");
                    SendSuccessResponseWithId("Camera added successfully", objectId);
                }
            }
            catch (Exception ex)
            {
                KKStudioSocketPlugin.Logger.LogError($"Add camera error: {ex.Message}");
                SendErrorResponse($"Add camera error: {ex.Message}");
            }
        }

        private void SendSuccessResponse(string message)
        {
            var response = new { type = "success", message = message };
            Send(Newtonsoft.Json.JsonConvert.SerializeObject(response));
        }

        private void SendSuccessResponseWithId(string message, int objectId)
        {
            var response = new { type = "success", message = message, objectId = objectId };
            Send(Newtonsoft.Json.JsonConvert.SerializeObject(response));
        }
        
        private void SendErrorResponse(string message)
        {
            var response = new { type = "error", message = message };
            Send(Newtonsoft.Json.JsonConvert.SerializeObject(response));
        }
    }
}