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
                if (cmd.command != "transform")
                {
                    KKStudioSocketPlugin.Logger.LogWarning($"Unsupported update command: {cmd.command}");
                    SendErrorResponse($"Unsupported update command: {cmd.command}");
                    return;
                }

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