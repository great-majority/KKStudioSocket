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
        
        public void Handle()
        {
            try
            {
                List<object> roots = new List<object>();

                foreach (var kv in Studio.Studio.Instance.dicInfo)
                {
                    var node = kv.Key;
                    var info = kv.Value;

                    if (node.parent == null)
                    {
                        roots.Add(BuildNodeJson(node, info));
                    }
                }

                var jsonResponse = JsonConvert.SerializeObject(roots);
                KKStudioSocketPlugin.Logger.LogDebug($"Tree command received, number of root objects: {roots.Count}");
                
                Send(jsonResponse);
            }
            catch (Exception ex)
            {
                KKStudioSocketPlugin.Logger.LogError($"Tree command processing error: {ex.Message}");
            }
        }

        private object BuildNodeJson(TreeNodeObject node, ObjectCtrlInfo info)
        {
            return new
            {
                name = node.textName,
                objectInfo = new
                {
                    id = info.objectInfo.dicKey,
                    type = info.GetType().Name
                },
                children = node.child
                    .Where(child => Studio.Studio.Instance.dicInfo.ContainsKey(child))
                    .Select(child => BuildNodeJson(child, Studio.Studio.Instance.dicInfo[child]))
                    .ToList()
            };
        }
    }
}