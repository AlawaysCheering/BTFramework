using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using BehaviorTreeEditor.Runtime.Core;

namespace BehaviorTreeEditor.Runtime.Data
{
    /// <summary>
    /// 行为树数据类 - 存储完整的行为树结构
    /// </summary>
    [Serializable]
    public class TreeData
    {
        [LabelText("树名称")]
        public string treeName = "New Behavior Tree";

        [LabelText("树描述")]
        [TextArea(2, 4)]
        public string description;

        [LabelText("根节点GUID")]
        [ReadOnly]
        public string rootNodeGuid;

        [LabelText("所有节点")]
        [DictionaryDrawerSettings(KeyLabel = "GUID", ValueLabel = "节点数据")]
        public Dictionary<string, NodeData> allNodes = new Dictionary<string, NodeData>();

        [LabelText("创建时间")]
        [ReadOnly]
        public string createTime;

        [LabelText("最后修改时间")]
        [ReadOnly]
        public string lastModifyTime;

        /// <summary>
        /// 获取指定GUID的节点数据
        /// </summary>
        public NodeData GetNodeData(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;
            return allNodes.TryGetValue(guid, out var data) ? data : null;
        }

        /// <summary>
        /// 获取树数据本身（用于接口统一）
        /// </summary>
        public TreeData GetTreeData()
        {
            return this;
        }

        /// <summary>
        /// 获取根节点数据
        /// </summary>
        public NodeData GetRootNodeData()
        {
            return GetNodeData(rootNodeGuid);
        }

        /// <summary>
        /// 添加节点
        /// </summary>
        public void AddNode(NodeData nodeData)
        {
            if (nodeData == null || string.IsNullOrEmpty(nodeData.guid)) return;
            
            allNodes[nodeData.guid] = nodeData;
            lastModifyTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // 如果是第一个节点且为Root类型，设为根节点
            if (nodeData.nodeType == NodeType.Root && string.IsNullOrEmpty(rootNodeGuid))
            {
                rootNodeGuid = nodeData.guid;
            }
        }

        /// <summary>
        /// 移除节点
        /// </summary>
        public void RemoveNode(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return;
            
            var nodeData = GetNodeData(guid);
            if (nodeData == null) return;

            // 从父节点的子节点列表中移除
            if (!string.IsNullOrEmpty(nodeData.parentGuid))
            {
                var parentData = GetNodeData(nodeData.parentGuid);
                parentData?.childrenGuids.Remove(guid);
            }

            // 递归移除所有子节点
            foreach (var childGuid in nodeData.childrenGuids.ToList())
            {
                RemoveNode(childGuid);
            }

            allNodes.Remove(guid);
            lastModifyTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// 连接两个节点
        /// </summary>
        public void ConnectNodes(string parentGuid, string childGuid)
        {
            var parentData = GetNodeData(parentGuid);
            var childData = GetNodeData(childGuid);

            if (parentData == null || childData == null) return;

            // 移除子节点原有的父节点关系
            if (!string.IsNullOrEmpty(childData.parentGuid))
            {
                var oldParent = GetNodeData(childData.parentGuid);
                oldParent?.childrenGuids.Remove(childGuid);
            }

            // 建立新关系
            if (!parentData.childrenGuids.Contains(childGuid))
            {
                parentData.childrenGuids.Add(childGuid);
            }
            childData.parentGuid = parentGuid;
            
            lastModifyTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// 断开两个节点的连接
        /// </summary>
        public void DisconnectNodes(string parentGuid, string childGuid)
        {
            var parentData = GetNodeData(parentGuid);
            var childData = GetNodeData(childGuid);

            if (parentData == null || childData == null) return;

            parentData.childrenGuids.Remove(childGuid);
            if (childData.parentGuid == parentGuid)
            {
                childData.parentGuid = null;
            }
            
            lastModifyTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// 获取所有指定类型的节点
        /// </summary>
        public List<NodeData> GetNodesByType(NodeType type)
        {
            return allNodes.Values.Where(n => n.nodeType == type).ToList();
        }

        /// <summary>
        /// 深拷贝树数据（用于运行时）
        /// </summary>
        public TreeData Clone()
        {
            var clone = new TreeData
            {
                treeName = this.treeName,
                description = this.description,
                rootNodeGuid = this.rootNodeGuid,
                createTime = this.createTime,
                lastModifyTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            foreach (var kvp in allNodes)
            {
                clone.allNodes[kvp.Key] = kvp.Value.Clone();
            }

            return clone;
        }

        /// <summary>
        /// 创建新的树数据
        /// </summary>
        public static TreeData Create(string name = "New Behavior Tree")
        {
            var treeData = new TreeData
            {
                treeName = name,
                createTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                lastModifyTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            // 创建根节点
            var rootNode = NodeData.Create(NodeType.Root, typeof(RootNode), Vector2.zero);
            treeData.AddNode(rootNode);
            treeData.rootNodeGuid = rootNode.guid;

            return treeData;
        }

        /// <summary>
        /// 验证树结构完整性
        /// </summary>
        public bool Validate(out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrEmpty(rootNodeGuid))
            {
                errorMessage = "行为树缺少根节点";
                return false;
            }

            if (!allNodes.ContainsKey(rootNodeGuid))
            {
                errorMessage = "根节点GUID无效";
                return false;
            }

            // 检查是否有循环引用
            var visited = new HashSet<string>();
            if (HasCycle(rootNodeGuid, visited))
            {
                errorMessage = "行为树存在循环引用";
                return false;
            }

            return true;
        }

        private bool HasCycle(string nodeGuid, HashSet<string> visited)
        {
            if (visited.Contains(nodeGuid)) return true;
            visited.Add(nodeGuid);

            var nodeData = GetNodeData(nodeGuid);
            if (nodeData == null) return false;

            foreach (var childGuid in nodeData.childrenGuids)
            {
                if (HasCycle(childGuid, new HashSet<string>(visited)))
                    return true;
            }

            return false;
        }
    }
}