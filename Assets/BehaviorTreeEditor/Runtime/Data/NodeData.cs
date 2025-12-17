using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace BehaviorTreeEditor.Runtime.Data
{
    /// <summary>
    /// 节点类型枚举
    /// </summary>
    public enum NodeType
    {
        Root,           // 根节点
        Composite,      // 组合节点
        Decorator,      // 装饰节点
        Action,         // 动作节点
        Condition       // 条件节点
    }

    /// <summary>
    /// 节点执行状态枚举
    /// </summary>
    public enum NodeState
    {
        Invalid,    // 无效/未执行
        Running,    // 执行中
        Success,    // 成功
        Failure     // 失败
    }

    /// <summary>
    /// 打断类型枚举 - 对标Behavior Designer的打断机制
    /// </summary>
    public enum AbortType
    {
        None,           // 无打断
        Self,           // 打断自身子树
        LowerPriority,  // 打断低优先级节点
        Both            // 两者都打断
    }

    /// <summary>
    /// 节点数据类 - 存储节点的序列化数据
    /// </summary>
    [Serializable]
    public class NodeData
    {
        [ReadOnly]
        [LabelText("节点GUID")]
        public string guid;

        [LabelText("节点类型")]
        [ReadOnly]
        public NodeType nodeType;

        [LabelText("节点名称")]
        public string nodeName;

        [LabelText("节点完整类型名")]
        [ReadOnly]
        public string nodeClassName;

        [LabelText("编辑器位置")]
        [ReadOnly]
        public Vector2 position;

        [LabelText("子节点GUID列表")]
        [ReadOnly]
        public List<string> childrenGuids = new List<string>();

        [LabelText("父节点GUID")]
        [ReadOnly]
        public string parentGuid;

        [LabelText("当前状态")]
        [ReadOnly]
        public NodeState nodeState = NodeState.Invalid;

        [LabelText("打断类型")]
        [ShowIf("@nodeType == NodeType.Composite")]
        public AbortType abortType = AbortType.None;

        [LabelText("节点描述")]
        [TextArea(2, 4)]
        public string description;

        [LabelText("自定义数据(JSON)")]
        [TextArea(3, 6)]
        public string customDataJson;

        /// <summary>
        /// 创建新节点数据
        /// </summary>
        public static NodeData Create(NodeType type, string className, Vector2 pos)
        {
            return new NodeData
            {
                guid = Guid.NewGuid().ToString(),
                nodeType = type,
                nodeClassName = className,
                nodeName = className.Replace("Node", ""),
                position = pos,
                nodeState = NodeState.Invalid
            };
        }

        /// <summary>
        /// 深拷贝节点数据
        /// </summary>
        public NodeData Clone()
        {
            return new NodeData
            {
                guid = this.guid,
                nodeType = this.nodeType,
                nodeName = this.nodeName,
                nodeClassName = this.nodeClassName,
                position = this.position,
                childrenGuids = new List<string>(this.childrenGuids),
                parentGuid = this.parentGuid,
                nodeState = NodeState.Invalid,
                abortType = this.abortType,
                description = this.description,
                customDataJson = this.customDataJson
            };
        }
    }
}