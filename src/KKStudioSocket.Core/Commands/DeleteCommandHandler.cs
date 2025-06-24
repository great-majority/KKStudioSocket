using System;
using System.Linq;
using WebSocketSharp;
using Studio;

namespace KKStudioSocket.Commands
{
    public class DeleteCommandHandler : BaseCommandHandler
    {
        public DeleteCommandHandler(System.Action<string> sendCallback) : base(sendCallback) { }
        
        public void Handle(DeleteCommand cmd)
        {
            try
            {
                // Parameter validation
                if (cmd.id < 0)
                {
                    KKStudioSocketPlugin.Logger.LogWarning($"Invalid object ID: {cmd.id}");
                    SendErrorResponse($"Invalid object ID: {cmd.id}");
                    return;
                }

                // Find object
                var oci = Studio.Studio.Instance.dicInfo.Values
                    .FirstOrDefault(info => info.objectInfo.dicKey == cmd.id);

                if (oci == null)
                {
                    KKStudioSocketPlugin.Logger.LogWarning($"Object with ID {cmd.id} not found");
                    SendErrorResponse($"Object with ID {cmd.id} not found");
                    return;
                }

                // Delete object through TreeNodeCtrl for proper UI update
                Studio.Studio.Instance.m_TreeNodeCtrl.DeleteNode(oci.treeNodeObject);

                KKStudioSocketPlugin.Logger.LogDebug($"Object {cmd.id} deleted successfully");
                SendSuccessResponse($"Object {cmd.id} deleted successfully");
            }
            catch (Exception ex)
            {
                KKStudioSocketPlugin.Logger.LogError($"Delete error: {ex.Message}");
                SendErrorResponse($"Delete error: {ex.Message}");
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