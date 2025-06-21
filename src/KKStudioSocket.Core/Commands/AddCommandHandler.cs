using System;
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
                    default:
                        KKStudioSocketPlugin.Logger.LogWarning($"Unsupported add command: {cmd.command}");
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
                if (cmd.group < 0 || cmd.category < 0 || cmd.no < 0)
                {
                    KKStudioSocketPlugin.Logger.LogWarning($"Invalid item parameters: group={cmd.group}, category={cmd.category}, no={cmd.no}");
                    SendErrorResponse($"Invalid item parameters: group={cmd.group}, category={cmd.category}, no={cmd.no}");
                    return;
                }

                // Call Studio.AddItem to add the item
                Studio.Studio.Instance.AddItem(cmd.group, cmd.category, cmd.no);
                
                KKStudioSocketPlugin.Logger.LogInfo($"Item added successfully: group={cmd.group}, category={cmd.category}, no={cmd.no}");
                SendSuccessResponse($"Item added successfully: group={cmd.group}, category={cmd.category}, no={cmd.no}");
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
                if (cmd.no < 0)
                {
                    KKStudioSocketPlugin.Logger.LogWarning($"Invalid light parameter: no={cmd.no}");
                    SendErrorResponse($"Invalid light parameter: no={cmd.no}");
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
                Studio.Studio.Instance.AddLight(cmd.no);
                
                KKStudioSocketPlugin.Logger.LogInfo($"Light added successfully: no={cmd.no}");
                SendSuccessResponse($"Light added successfully: no={cmd.no}");
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