using System;
using System.Collections.Generic;
using System.Linq;
using WebSocketSharp;
using Newtonsoft.Json;
using Studio;

namespace KKStudioSocket.Commands
{
    public class TreeCommandHandler : BaseCommandHandler
    {
        public TreeCommandHandler(System.Action<string> sendCallback) : base(sendCallback) { }
        
        public void Handle(TreeCommand cmd = null)
        {
            try
            {
                int? maxDepth = cmd?.depth;
                int? startId = cmd?.id;

                if (startId.HasValue)
                {
                    // Find specific object and return its subtree
                    var targetNode = Studio.Studio.Instance.dicInfo.Keys.FirstOrDefault(node => 
                        Studio.Studio.Instance.dicInfo[node].objectInfo.dicKey == startId.Value);
                    
                    if (targetNode != null)
                    {
                        var targetInfo = Studio.Studio.Instance.dicInfo[targetNode];
                        var result = BuildNodeJson(targetNode, targetInfo, maxDepth, 0);
                        var jsonResponse = JsonConvert.SerializeObject(result);
                        KKStudioSocketPlugin.Logger.LogDebug($"Tree command received for specific object ID: {startId}, maxDepth: {maxDepth?.ToString() ?? "unlimited"}");
                        Send(jsonResponse);
                    }
                    else
                    {
                        // Object not found
                        var errorResponse = JsonConvert.SerializeObject(new { 
                            type = "error", 
                            message = $"Object with ID {startId} not found" 
                        });
                        Send(errorResponse);
                        KKStudioSocketPlugin.Logger.LogWarning($"Tree command: Object with ID {startId} not found");
                    }
                }
                else
                {
                    // Default behavior: return all root objects
                    List<object> roots = new List<object>();

                    foreach (var kv in Studio.Studio.Instance.dicInfo)
                    {
                        var node = kv.Key;
                        var info = kv.Value;

                        if (node.parent == null)
                        {
                            roots.Add(BuildNodeJson(node, info, maxDepth, 0));
                        }
                    }

                    var jsonResponse = JsonConvert.SerializeObject(roots);
                    KKStudioSocketPlugin.Logger.LogDebug($"Tree command received, number of root objects: {roots.Count}, maxDepth: {maxDepth?.ToString() ?? "unlimited"}");
                    Send(jsonResponse);
                }
            }
            catch (Exception ex)
            {
                KKStudioSocketPlugin.Logger.LogError($"Tree command processing error: {ex.Message}");
            }
        }

        // Backward compatibility
        public void Handle()
        {
            Handle(null);
        }

        private object BuildNodeJson(TreeNodeObject node, ObjectCtrlInfo info, int? maxDepth, int currentDepth)
        {
            // Get transform information
            var transform = new
            {
                pos = new float[] { 
                    info.objectInfo.changeAmount.pos.x, 
                    info.objectInfo.changeAmount.pos.y, 
                    info.objectInfo.changeAmount.pos.z 
                },
                rot = new float[] { 
                    info.objectInfo.changeAmount.rot.x, 
                    info.objectInfo.changeAmount.rot.y, 
                    info.objectInfo.changeAmount.rot.z 
                },
                scale = new float[] { 
                    info.objectInfo.changeAmount.scale.x, 
                    info.objectInfo.changeAmount.scale.y, 
                    info.objectInfo.changeAmount.scale.z 
                }
            };

            // Build children only if depth limit not reached
            var children = new List<object>();
            if (maxDepth == null || currentDepth < maxDepth)
            {
                children = node.child
                    .Where(child => Studio.Studio.Instance.dicInfo.ContainsKey(child))
                    .Select(child => BuildNodeJson(child, Studio.Studio.Instance.dicInfo[child], maxDepth, currentDepth + 1))
                    .ToList();
            }

            var baseInfo = new
            {
                name = node.textName,
                objectInfo = new
                {
                    id = info.objectInfo.dicKey,
                    type = info.GetType().Name,
                    transform = transform
                },
                children = children
            };

            // Add detailed information for items
            if (info is OCIItem itemInfo && itemInfo.itemInfo != null)
            {
                return new
                {
                    name = baseInfo.name,
                    objectInfo = new
                    {
                        id = baseInfo.objectInfo.id,
                        type = baseInfo.objectInfo.type,
                        transform = baseInfo.objectInfo.transform,
                        itemDetail = new
                        {
                            group = itemInfo.itemInfo.group,
                            category = itemInfo.itemInfo.category,
                            itemId = itemInfo.itemInfo.no
                        }
                    },
                    children = baseInfo.children
                };
            }

            return baseInfo;
        }
    }
}