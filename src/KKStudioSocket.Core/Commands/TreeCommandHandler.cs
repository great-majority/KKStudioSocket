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
                List<object> roots = new List<object>();
                int? maxDepth = cmd?.depth;

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