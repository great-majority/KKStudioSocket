using System;
using System.Linq;
using WebSocketSharp;
using Studio;
using Manager;

namespace KKStudioSocket.Commands
{
    public class HierarchyCommandHandler : BaseCommandHandler
    {
        public HierarchyCommandHandler(System.Action<string> sendCallback) : base(sendCallback) { }

        public void Handle(HierarchyCommand cmd)
        {
            ApplyHierarchy(cmd);
        }

        private void ApplyHierarchy(HierarchyCommand cmd)
        {
            try
            {
                switch (cmd.command?.ToLower())
                {
                    case "attach":
                        HandleAttach(cmd);
                        break;
                    case "detach":
                        HandleDetach(cmd);
                        break;
                    default:
                        KKStudioSocketPlugin.Logger.LogWarning($"Unsupported hierarchy command: {cmd.command}");
                        SendErrorResponse($"Unsupported hierarchy command: {cmd.command}");
                        break;
                }
            }
            catch (Exception ex)
            {
                KKStudioSocketPlugin.Logger.LogError($"Hierarchy command error: {ex.Message}");
                SendErrorResponse($"Hierarchy command error: {ex.Message}");
            }
        }

        private void HandleAttach(HierarchyCommand cmd)
        {
            try
            {
                // Parameter validation
                if (cmd.childId < 0)
                {
                    KKStudioSocketPlugin.Logger.LogWarning($"Invalid child ID: {cmd.childId}");
                    SendErrorResponse($"Invalid child ID: {cmd.childId}");
                    return;
                }

                if (cmd.parentId < 0)
                {
                    KKStudioSocketPlugin.Logger.LogWarning("Invalid parent ID. Use 'detach' command to remove parent.");
                    SendErrorResponse("Invalid parent ID. Use 'detach' command to remove parent.");
                    return;
                }

                // Find child object
                var childOci = Studio.Studio.Instance.dicInfo.Values
                    .FirstOrDefault(info => info.objectInfo.dicKey == cmd.childId);

                if (childOci == null)
                {
                    KKStudioSocketPlugin.Logger.LogWarning($"Child object with ID {cmd.childId} not found");
                    SendErrorResponse($"Child object with ID {cmd.childId} not found");
                    return;
                }

                // Find parent object
                var parentOci = Studio.Studio.Instance.dicInfo.Values
                    .FirstOrDefault(info => info.objectInfo.dicKey == cmd.parentId);

                if (parentOci == null)
                {
                    KKStudioSocketPlugin.Logger.LogWarning($"Parent object with ID {cmd.parentId} not found");
                    SendErrorResponse($"Parent object with ID {cmd.parentId} not found");
                    return;
                }

                // Check if parent can accept children
                // Most objects can be parents (folders, items, characters)
                // Lights have empty OnAttach implementation but method exists
                if (parentOci == null)
                {
                    KKStudioSocketPlugin.Logger.LogWarning($"Parent object {cmd.parentId} not found");
                    SendErrorResponse($"Parent object {cmd.parentId} not found");
                    return;
                }

                // Attach child to parent
                parentOci.OnAttach(parentOci.treeNodeObject, childOci);

                // Trigger change amount update for visual refresh                     
                childOci.objectInfo.changeAmount.OnChange();

                Studio.Studio.Instance.m_TreeNodeCtrl.SetParent(childOci.treeNodeObject, parentOci.treeNodeObject);

                KKStudioSocketPlugin.Logger.LogInfo($"Object {cmd.childId} attached to parent {cmd.parentId}");
                SendSuccessResponse($"Object {cmd.childId} attached to parent {cmd.parentId}");
            }
            catch (Exception ex)
            {
                KKStudioSocketPlugin.Logger.LogError($"Set parent error: {ex.Message}");
                SendErrorResponse($"Set parent error: {ex.Message}");
            }
        }

        private void HandleDetach(HierarchyCommand cmd)
        {
            try
            {
                // Parameter validation
                if (cmd.childId < 0)
                {
                    KKStudioSocketPlugin.Logger.LogWarning($"Invalid object ID: {cmd.childId}");
                    SendErrorResponse($"Invalid object ID: {cmd.childId}");
                    return;
                }

                // Find object
                var oci = Studio.Studio.Instance.dicInfo.Values
                    .FirstOrDefault(info => info.objectInfo.dicKey == cmd.childId);

                if (oci == null)
                {
                    KKStudioSocketPlugin.Logger.LogWarning($"Object with ID {cmd.childId} not found");
                    SendErrorResponse($"Object with ID {cmd.childId} not found");
                    return;
                }

                // Detach from parent
                if (oci.parentInfo != null)
                {
                    KKStudioSocketPlugin.Logger.LogDebug("Starting OnDetach...");
                    oci.OnDetach();
                    KKStudioSocketPlugin.Logger.LogDebug("OnDetach completed");

                    // Update UI tree to reflect detachment
                    if (oci.treeNodeObject != null)
                    {
                        try
                        {
                            KKStudioSocketPlugin.Logger.LogDebug("Updating UI tree...");
                            var treeCtrl = Studio.Studio.Instance.m_TreeNodeCtrl;
                            treeCtrl.SelectSingle(oci.treeNodeObject);
                            treeCtrl.RemoveNode();
                            KKStudioSocketPlugin.Logger.LogDebug("UI tree update completed");
                        }
                        catch (Exception uiEx)
                        {
                            KKStudioSocketPlugin.Logger.LogWarning($"UI update failed, but detach succeeded: {uiEx.Message}");
                            // Continue - detach operation was successful even if UI update failed
                        }
                    }

                    KKStudioSocketPlugin.Logger.LogInfo($"Object {cmd.childId} detached from parent");
                    SendSuccessResponse($"Object {cmd.childId} detached from parent");
                }
                else
                {
                    SendSuccessResponse($"Object {cmd.childId} already has no parent");
                }
            }
            catch (Exception ex)
            {
                KKStudioSocketPlugin.Logger.LogError($"Detach error: {ex.Message}");
                SendErrorResponse($"Detach error: {ex.Message}");
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