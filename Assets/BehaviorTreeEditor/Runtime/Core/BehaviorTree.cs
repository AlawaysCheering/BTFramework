using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using BehaviorTreeEditor.Runtime.Data;

namespace BehaviorTreeEditor.Runtime.Core
{
    /// <summary>
    /// 行为树运行时类 - 管理行为树的执行
    /// </summary>
    public class BehaviorTree
    {
        // 树数据
        public TreeData TreeData { get; private set; }
        
        // 黑板数据
        public BlackboardData Blackboard { get; private set; }
        
        // 根节点
        public BTNode RootNode { get; private set; }
        
        // 所有节点实例映射
        private Dictionary<string, BTNode> nodeInstances = new Dictionary<string, BTNode>();
        
        // 需要重评估的条件节点（用于打断机制）
        private List<BTNode> conditionalNodes = new List<BTNode>();
        
        // 当前正在执行的节点
        public BTNode CurrentRunningNode { get; private set; }

        // 节点状态变化事件（用于编辑器可视化）
        public event Action<BTNode, NodeState> OnNodeStateChanged;

        /// <summary>
        /// 初始化行为树
        /// </summary>
        public void Initialize(TreeData treeData, BlackboardData blackboard)
        {
            TreeData = treeData;
            Blackboard = blackboard;

            // 订阅黑板变量变化事件
            if (Blackboard != null)
            {
                Blackboard.OnVariableChanged += OnBlackboardVariableChanged;
            }

            // 构建节点树
            BuildNodeTree();
        }

        /// <summary>
        /// 构建节点树
        /// </summary>
        private void BuildNodeTree()
        {
            nodeInstances.Clear();
            conditionalNodes.Clear();

            if (TreeData == null || string.IsNullOrEmpty(TreeData.rootNodeGuid))
            {
                Debug.LogError("[BehaviorTree] Invalid tree data");
                return;
            }

            // 创建所有节点实例
            foreach (var kvp in TreeData.allNodes)
            {
                var nodeData = kvp.Value;
                var node = CreateNodeInstance(nodeData);
                if (node != null)
                {
                    node.Initialize(nodeData, this);
                    nodeInstances[kvp.Key] = node;

                    // 收集条件节点
                    if (nodeData.nodeType == NodeType.Condition)
                    {
                        conditionalNodes.Add(node);
                    }
                }
            }

            // 建立父子关系
            foreach (var kvp in TreeData.allNodes)
            {
                var nodeData = kvp.Value;
                if (!nodeInstances.TryGetValue(kvp.Key, out var node)) continue;

                foreach (var childGuid in nodeData.childrenGuids)
                {
                    if (nodeInstances.TryGetValue(childGuid, out var childNode))
                    {
                        node.AddChild(childNode);
                    }
                }
            }

            // 设置根节点
            if (nodeInstances.TryGetValue(TreeData.rootNodeGuid, out var root))
            {
                RootNode = root;
            }
        }

        /// <summary>
        /// 创建节点实例
        /// </summary>
        private BTNode CreateNodeInstance(NodeData nodeData)
        {
            if (string.IsNullOrEmpty(nodeData.nodeClassName))
            {
                return nodeData.nodeType == NodeType.Root ? new RootNode() : null;
            }

            // 通过反射创建节点实例
            var nodeType = FindNodeType(nodeData.nodeClassName);
            if (nodeType == null)
            {
                Debug.LogWarning($"[BehaviorTree] Node type not found: {nodeData.nodeClassName}");
                return null;
            }

            return Activator.CreateInstance(nodeType) as BTNode;
        }

        /// <summary>
        /// 查找节点类型
        /// </summary>
        private Type FindNodeType(string className)
        {
            // 在所有程序集中查找
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetTypes()
                    .FirstOrDefault(t => t.Name == className && typeof(BTNode).IsAssignableFrom(t));
                if (type != null) return type;
            }
            return null;
        }

        /// <summary>
        /// 每帧更新（Tick）
        /// </summary>
        public NodeState Tick()
        {
            if (RootNode == null)
            {
                Debug.LogError("[BehaviorTree] Root node is null");
                return NodeState.Failure;
            }

            // 检查打断条件
            CheckAbortConditions();

            // 执行行为树
            var state = RootNode.Evaluate();

            return state;
        }

        /// <summary>
        /// 检查打断条件
        /// </summary>
        private void CheckAbortConditions()
        {
            // 遍历所有可能触发打断的组合节点
            foreach (var kvp in nodeInstances)
            {
                var node = kvp.Value;
                var nodeData = TreeData.GetNodeData(kvp.Key);

                if (nodeData?.nodeType != NodeType.Composite) continue;
                if (nodeData.abortType == AbortType.None) continue;

                // 检查该组合节点下的条件节点
                CheckCompositeAbort(node, nodeData);
            }
        }

        /// <summary>
        /// 检查组合节点的打断条件
        /// </summary>
        private void CheckCompositeAbort(BTNode compositeNode, NodeData compositeData)
        {
            var abortType = compositeData.abortType;

            // 获取组合节点下的条件子节点
            var conditionChildren = compositeNode.Children
                .Where(c => c.NodeData?.nodeType == NodeType.Condition)
                .ToList();

            foreach (var conditionNode in conditionChildren)
            {
                // 临时评估条件节点
                var prevState = conditionNode.State;
                var conditionResult = EvaluateConditionWithoutStateChange(conditionNode);

                // 检查是否需要打断
                bool shouldAbort = false;

                switch (abortType)
                {
                    case AbortType.Self:
                        // 自我打断：条件从成功变为失败
                        if (prevState == NodeState.Success && conditionResult == NodeState.Failure)
                        {
                            shouldAbort = IsExecutingInSubtree(compositeNode);
                        }
                        break;

                    case AbortType.LowerPriority:
                        // 低优先级打断：条件变为成功时，打断低优先级分支
                        if (conditionResult == NodeState.Success)
                        {
                            shouldAbort = IsExecutingLowerPriorityBranch(compositeNode);
                        }
                        break;

                    case AbortType.Both:
                        // 两者都检查
                        if (prevState == NodeState.Success && conditionResult == NodeState.Failure)
                        {
                            shouldAbort = IsExecutingInSubtree(compositeNode);
                        }
                        else if (conditionResult == NodeState.Success)
                        {
                            shouldAbort = IsExecutingLowerPriorityBranch(compositeNode);
                        }
                        break;
                }

                if (shouldAbort)
                {
                    PerformAbort(compositeNode);
                    break;
                }
            }
        }

        /// <summary>
        /// 评估条件节点但不改变状态
        /// </summary>
        private NodeState EvaluateConditionWithoutStateChange(BTNode conditionNode)
        {
            // 保存当前状态
            var currentState = conditionNode.State;
            
            // 临时评估
            var result = conditionNode.Evaluate();
            
            // 如果需要保持原状态，可在此恢复
            // conditionNode.NodeData.nodeState = currentState;
            
            return result;
        }

        /// <summary>
        /// 检查当前是否在指定子树中执行
        /// </summary>
        private bool IsExecutingInSubtree(BTNode subtreeRoot)
        {
            if (CurrentRunningNode == null) return false;

            var node = CurrentRunningNode;
            while (node != null)
            {
                if (node == subtreeRoot) return true;
                node = node.Parent;
            }
            return false;
        }

        /// <summary>
        /// 检查是否正在执行低优先级分支
        /// </summary>
        private bool IsExecutingLowerPriorityBranch(BTNode compositeNode)
        {
            if (CurrentRunningNode == null) return false;

            // 获取当前执行节点在父节点中的索引
            var parent = compositeNode.Parent;
            if (parent == null) return false;

            int compositeIndex = parent.Children.IndexOf(compositeNode);
            
            // 检查CurrentRunningNode是否在更低优先级（更大索引）的分支中
            var node = CurrentRunningNode;
            while (node != null && node != parent)
            {
                var nodeParent = node.Parent;
                if (nodeParent == parent)
                {
                    int nodeIndex = parent.Children.IndexOf(node);
                    return nodeIndex > compositeIndex;
                }
                node = nodeParent;
            }

            return false;
        }

        /// <summary>
        /// 执行打断
        /// </summary>
        private void PerformAbort(BTNode fromNode)
        {
            Debug.Log($"[BehaviorTree] Abort triggered from: {fromNode.NodeData?.nodeName}");

            // 停止当前执行的节点
            if (CurrentRunningNode != null)
            {
                CurrentRunningNode.Abort();
            }

            // 重置从根节点到触发节点路径上的所有节点
            ResetSubtree(RootNode);
        }

        /// <summary>
        /// 重置子树
        /// </summary>
        private void ResetSubtree(BTNode node)
        {
            node?.Reset();
        }

        /// <summary>
        /// 黑板变量变化回调
        /// </summary>
        private void OnBlackboardVariableChanged(string name, object oldValue, object newValue)
        {
            // 可以在此触发依赖该变量的条件节点重评估
            Debug.Log($"[BehaviorTree] Blackboard variable changed: {name} = {newValue}");
        }

        /// <summary>
        /// 获取节点实例
        /// </summary>
        public BTNode GetNode(string guid)
        {
            return nodeInstances.TryGetValue(guid, out var node) ? node : null;
        }

        /// <summary>
        /// 获取所有节点实例
        /// </summary>
        public IEnumerable<BTNode> GetAllNodes()
        {
            return nodeInstances.Values;
        }

        /// <summary>
        /// 设置当前运行节点（由节点自己调用）
        /// </summary>
        public void SetCurrentRunningNode(BTNode node)
        {
            CurrentRunningNode = node;
            OnNodeStateChanged?.Invoke(node, node.State);
        }

        /// <summary>
        /// 清理
        /// </summary>
        public void Cleanup()
        {
            if (Blackboard != null)
            {
                Blackboard.OnVariableChanged -= OnBlackboardVariableChanged;
            }
            nodeInstances.Clear();
            conditionalNodes.Clear();
            RootNode = null;
            CurrentRunningNode = null;
        }
    }
}