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

                // Call Studio.AddItem to add the item
                Studio.Studio.Instance.AddItem(cmd.group, cmd.category, cmd.itemId);
                
                KKStudioSocketPlugin.Logger.LogInfo($"Item added successfully: group={cmd.group}, category={cmd.category}, itemId={cmd.itemId}");
                SendSuccessResponse($"Item added successfully: group={cmd.group}, category={cmd.category}, itemId={cmd.itemId}");
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

                // Call Studio.AddLight to add the light
                Studio.Studio.Instance.AddLight(cmd.lightId);
                
                KKStudioSocketPlugin.Logger.LogInfo($"Light added successfully: lightId={cmd.lightId}");
                SendSuccessResponse($"Light added successfully: lightId={cmd.lightId}");
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
                        Studio.Studio.Instance.AddFemale(cmd.path);
                        KKStudioSocketPlugin.Logger.LogInfo($"Female character added successfully: {cmd.path}");
                        SendSuccessResponse($"Female character added successfully: {cmd.path}");
                        break;
                    case "male":
                        Studio.Studio.Instance.AddMale(cmd.path);
                        KKStudioSocketPlugin.Logger.LogInfo($"Male character added successfully: {cmd.path}");
                        SendSuccessResponse($"Male character added successfully: {cmd.path}");
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
                // Call Studio.AddFolder to add the folder
                Studio.Studio.Instance.AddFolder();
                
                // If name is specified, try to rename the newly created folder
                if (!string.IsNullOrEmpty(cmd.name))
                {
                    // Get the last added folder (most recently created)
                    var folders = Studio.Studio.Instance.dicInfo.Values
                        .Where(info => info is Studio.OCIFolder)
                        .Cast<Studio.OCIFolder>()
                        .OrderByDescending(f => f.objectInfo.dicKey)
                        .FirstOrDefault();
                    
                    if (folders != null)
                    {
                        folders.name = cmd.name;
                        KKStudioSocketPlugin.Logger.LogInfo($"Folder added and renamed to: {cmd.name}");
                        SendSuccessResponse($"Folder added successfully with name: {cmd.name}");
                        return;
                    }
                }
                
                KKStudioSocketPlugin.Logger.LogInfo("Folder added successfully");
                SendSuccessResponse("Folder added successfully");
            }
            catch (Exception ex)
            {
                KKStudioSocketPlugin.Logger.LogError($"Add folder error: {ex.Message}");
                SendErrorResponse($"Add folder error: {ex.Message}");
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