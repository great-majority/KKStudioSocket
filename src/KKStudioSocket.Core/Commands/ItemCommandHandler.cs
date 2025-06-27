using System;
using System.Collections.Generic;
using System.Linq;
using WebSocketSharp;
using Newtonsoft.Json;
using Studio;

namespace KKStudioSocket.Commands
{
    public class ItemCommandHandler : BaseCommandHandler
    {
        public ItemCommandHandler(System.Action<string> sendCallback) : base(sendCallback) { }
        
        public void Handle(ItemCommand cmd)
        {
            ApplyItemCommand(cmd);
        }

        private void ApplyItemCommand(ItemCommand cmd)
        {
            try
            {
                switch (cmd.command?.ToLower())
                {
                    case "list-groups":
                        HandleListGroups(cmd);
                        break;
                    case "list-group":
                        HandleListGroup(cmd);
                        break;
                    case "list-category":
                        HandleListCategory(cmd);
                        break;
                    case "catalog":
                        HandleCatalog(cmd);
                        break;
                    default:
                        KKStudioSocketPlugin.Logger.LogWarning($"Unsupported item command: {cmd.command}");
                        SendErrorResponse($"Unsupported item command: {cmd.command}");
                        break;
                }
            }
            catch (Exception ex)
            {
                KKStudioSocketPlugin.Logger.LogError($"Item command error: {ex.Message}");
                SendErrorResponse($"Item command error: {ex.Message}");
            }
        }

        private void HandleListGroups(ItemCommand cmd)
        {
            try
            {
                var groups = GetItemGroups();
                
                var response = new
                {
                    type = "success",
                    command = "list-groups",
                    data = groups
                };

                Send(JsonConvert.SerializeObject(response));
                KKStudioSocketPlugin.Logger.LogDebug($"Item groups retrieved: {groups.Count} groups");
            }
            catch (Exception ex)
            {
                KKStudioSocketPlugin.Logger.LogError($"List groups error: {ex.Message}");
                SendErrorResponse($"List groups error: {ex.Message}");
            }
        }

        private void HandleListGroup(ItemCommand cmd)
        {
            try
            {
                if (cmd.groupId < 0)
                {
                    SendErrorResponse("Group ID is required and must be non-negative");
                    return;
                }

                var groupData = GetGroupData(cmd.groupId);
                if (groupData == null)
                {
                    SendErrorResponse($"Group with ID {cmd.groupId} not found");
                    return;
                }

                var response = new
                {
                    type = "success", 
                    command = "list-group",
                    groupId = cmd.groupId,
                    data = groupData
                };

                Send(JsonConvert.SerializeObject(response));
                KKStudioSocketPlugin.Logger.LogDebug($"Group {cmd.groupId} data retrieved");
            }
            catch (Exception ex)
            {
                KKStudioSocketPlugin.Logger.LogError($"List group error: {ex.Message}");
                SendErrorResponse($"List group error: {ex.Message}");
            }
        }

        private void HandleListCategory(ItemCommand cmd)
        {
            try
            {
                if (cmd.groupId < 0 || cmd.categoryId < 0)
                {
                    SendErrorResponse("Both group ID and category ID are required and must be non-negative");
                    return;
                }

                var categoryData = GetCategoryData(cmd.groupId, cmd.categoryId);
                if (categoryData == null)
                {
                    SendErrorResponse($"Category {cmd.categoryId} in group {cmd.groupId} not found");
                    return;
                }

                var response = new
                {
                    type = "success",
                    command = "list-category", 
                    groupId = cmd.groupId,
                    categoryId = cmd.categoryId,
                    data = categoryData
                };

                Send(JsonConvert.SerializeObject(response));
                KKStudioSocketPlugin.Logger.LogDebug($"Category {cmd.categoryId} in group {cmd.groupId} data retrieved");
            }
            catch (Exception ex)
            {
                KKStudioSocketPlugin.Logger.LogError($"List category error: {ex.Message}");
                SendErrorResponse($"List category error: {ex.Message}");
            }
        }

        private void HandleCatalog(ItemCommand cmd)
        {
            try
            {
                var catalog = BuildItemCatalog();
                var jsonResponse = JsonConvert.SerializeObject(catalog);
                
                KKStudioSocketPlugin.Logger.LogDebug($"Item catalog retrieved with {catalog.Count} groups");
                Send(jsonResponse);
            }
            catch (Exception ex)
            {
                KKStudioSocketPlugin.Logger.LogError($"Item catalog retrieval error: {ex.Message}");
                SendErrorResponse($"Item catalog retrieval error: {ex.Message}");
            }
        }

        private List<object> BuildItemCatalog()
        {
            var catalog = new List<object>();
            
            // Get singleton Info instance
            var info = Singleton<Info>.Instance;
            if (info?.dicItemGroupCategory == null)
            {
                KKStudioSocketPlugin.Logger.LogWarning("Item group category dictionary is null");
                return catalog;
            }

            // Iterate through all groups
            foreach (var groupPair in info.dicItemGroupCategory)
            {
                int groupId = groupPair.Key;
                var groupInfo = groupPair.Value;
                
                var groupData = new
                {
                    id = groupId,
                    name = groupInfo.name,
                    type = "group",
                    categories = BuildCategoriesForGroup(groupId, groupInfo)
                };
                
                catalog.Add(groupData);
            }
            
            return catalog;
        }

        private List<object> BuildCategoriesForGroup(int groupId, Info.GroupInfo groupInfo)
        {
            var categories = new List<object>();
            
            if (groupInfo.dicCategory == null)
            {
                return categories;
            }

            // Iterate through categories in this group
            foreach (var categoryPair in groupInfo.dicCategory)
            {
                int categoryId = categoryPair.Key;
                string categoryName = categoryPair.Value;
                
                var categoryData = new
                {
                    id = categoryId,
                    name = categoryName,
                    type = "category",
                    items = BuildItemsForCategory(groupId, categoryId)
                };
                
                categories.Add(categoryData);
            }
            
            return categories;
        }

        private List<object> BuildItemsForCategory(int groupId, int categoryId)
        {
            var items = new List<object>();
            
            var info = Singleton<Info>.Instance;
            
            // Check if this group/category combination exists in dicItemLoadInfo
            if (info?.dicItemLoadInfo == null || 
                !info.dicItemLoadInfo.ContainsKey(groupId) ||
                !info.dicItemLoadInfo[groupId].ContainsKey(categoryId))
            {
                return items;
            }

            // Iterate through items in this category
            foreach (var itemPair in info.dicItemLoadInfo[groupId][categoryId])
            {
                int itemId = itemPair.Key;
                var itemInfo = itemPair.Value;
                
                var itemData = new
                {
                    id = itemId,
                    name = itemInfo.name,
                    type = "item",
                    groupId = groupId,
                    categoryId = categoryId,
                    properties = new
                    {
                        isAnime = itemInfo.isAnime,
                        isScale = itemInfo.isScale,
                        isEmission = itemInfo.isEmission,
                        isGlass = itemInfo.isGlass,
                        hasColor = itemInfo.isColor,
                        hasPattern = itemInfo.isPattren,
                        colorSlots = itemInfo.color?.Length ?? 0,
                        patternSlots = itemInfo.pattren?.Length ?? 0,
                        childRoot = itemInfo.childRoot,
                        bones = itemInfo.bones?.Count ?? 0
                    },
                    file = new
                    {
                        manifest = itemInfo.manifest,
                        bundlePath = itemInfo.bundlePath,
                        fileName = itemInfo.fileName
                    }
                };
                
                items.Add(itemData);
            }
            
            return items;
        }

        private List<object> GetItemGroups()
        {
            var groups = new List<object>();
            
            var info = Singleton<Info>.Instance;
            if (info?.dicItemGroupCategory == null)
            {
                return groups;
            }

            foreach (var groupPair in info.dicItemGroupCategory)
            {
                int groupId = groupPair.Key;
                var groupInfo = groupPair.Value;
                
                var groupData = new
                {
                    id = groupId,
                    name = groupInfo.name,
                    categoryCount = groupInfo.dicCategory?.Count ?? 0
                };
                
                groups.Add(groupData);
            }
            
            return groups.OrderBy(g => {
                var obj = g.GetType().GetProperty("id").GetValue(g, null);
                return (int)obj;
            }).ToList();
        }

        private object GetGroupData(int groupId)
        {
            var info = Singleton<Info>.Instance;
            if (info?.dicItemGroupCategory == null || !info.dicItemGroupCategory.ContainsKey(groupId))
            {
                return null;
            }

            var groupInfo = info.dicItemGroupCategory[groupId];
            var categories = new List<object>();

            if (groupInfo.dicCategory != null)
            {
                foreach (var categoryPair in groupInfo.dicCategory)
                {
                    int categoryId = categoryPair.Key;
                    string categoryName = categoryPair.Value;
                    
                    // Count items in this category
                    int itemCount = 0;
                    if (info.dicItemLoadInfo?.ContainsKey(groupId) == true && 
                        info.dicItemLoadInfo[groupId]?.ContainsKey(categoryId) == true)
                    {
                        itemCount = info.dicItemLoadInfo[groupId][categoryId].Count;
                    }

                    var categoryData = new
                    {
                        id = categoryId,
                        name = categoryName,
                        itemCount = itemCount
                    };
                    
                    categories.Add(categoryData);
                }
            }

            return new
            {
                id = groupId,
                name = groupInfo.name,
                categories = categories.OrderBy(c => {
                    var obj = c.GetType().GetProperty("id").GetValue(c, null);
                    return (int)obj;
                }).ToList()
            };
        }

        private object GetCategoryData(int groupId, int categoryId)
        {
            var info = Singleton<Info>.Instance;
            
            // Check if group/category combination exists
            if (info?.dicItemLoadInfo == null || 
                !info.dicItemLoadInfo.ContainsKey(groupId) ||
                !info.dicItemLoadInfo[groupId].ContainsKey(categoryId))
            {
                return null;
            }

            var items = new List<object>();

            foreach (var itemPair in info.dicItemLoadInfo[groupId][categoryId])
            {
                int itemId = itemPair.Key;
                var itemInfo = itemPair.Value;
                
                var itemData = new
                {
                    id = itemId,
                    name = itemInfo.name,
                    properties = new
                    {
                        isAnime = itemInfo.isAnime,
                        isScale = itemInfo.isScale,
                        isEmission = itemInfo.isEmission,
                        isGlass = itemInfo.isGlass,
                        hasColor = itemInfo.isColor,
                        hasPattern = itemInfo.isPattren,
                        colorSlots = itemInfo.color?.Length ?? 0,
                        patternSlots = itemInfo.pattren?.Length ?? 0,
                        childRoot = itemInfo.childRoot,
                        bones = itemInfo.bones?.Count ?? 0
                    }
                };
                
                items.Add(itemData);
            }

            // Get category name
            string categoryName = "";
            if (info.dicItemGroupCategory?.ContainsKey(groupId) == true)
            {
                var groupInfo = info.dicItemGroupCategory[groupId];
                if (groupInfo.dicCategory?.ContainsKey(categoryId) == true)
                {
                    categoryName = groupInfo.dicCategory[categoryId];
                }
            }

            return new
            {
                id = categoryId,
                name = categoryName,
                groupId = groupId,
                items = items.OrderBy(i => {
                    var obj = i.GetType().GetProperty("id").GetValue(i, null);
                    return (int)obj;
                }).ToList()
            };
        }
        
        private void SendErrorResponse(string message)
        {
            var response = new { type = "error", message = message };
            Send(JsonConvert.SerializeObject(response));
        }
    }
}