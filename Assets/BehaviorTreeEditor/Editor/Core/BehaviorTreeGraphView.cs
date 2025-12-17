using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using BehaviorTreeEditor.Runtime.Core;
using BehaviorTreeEditor.Runtime.Data;
using BehaviorTreeEditor.Runtime.Attributes;

namespace BehaviorTreeEditor.Editor.Core
{
    /// <summary>
    /// 行为树GraphView实现
    /// </summary>
    public class BehaviorTreeGraphView : GraphView
    {
        private BehaviorTreeEditorWindow editorWindow;
        private BehaviorTreeSO currentAsset;
        private Dictionary<string, BTNodeView> nodeViews = new Dictionary<string, BTNodeView>();
        private bool isRuntimeMode = false;
        private bool isLoadingAsset = false;

        // 节点类型到创建方法的映射
        private Dictionary<Type, Func<Vector2, BTNodeView>> nodeCreators;

        public BehaviorTreeGraphView(BehaviorTreeEditorWindow window)
        {
            editorWindow = window;

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            graphViewChanged = OnGraphViewChanged;
            serializeGraphElements = OnSerializeGraphElements;
            unserializeAndPaste = OnUnserializeAndPaste;

            InitializeNodeCreators();
            RegisterCallback<ContextualMenuPopulateEvent>(OnContextMenuPopulate);
        }

        private void InitializeNodeCreators()
        {
            nodeCreators = new Dictionary<Type, Func<Vector2, BTNodeView>>();

            var nodeTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.GetCustomAttributes(typeof(BTNodeAttribute), false).Length > 0)
                .Where(t => typeof(BTNode).IsAssignableFrom(t));

            foreach (var type in nodeTypes)
            {
                var attr = (BTNodeAttribute)type.GetCustomAttributes(typeof(BTNodeAttribute), false)[0];
                var nodeType = type;

                nodeCreators[nodeType] = (pos) => CreateNodeViewForType(nodeType, attr, pos);
            }
        }

        private BTNodeView CreateNodeViewForType(Type nodeType, BTNodeAttribute attr, Vector2 position)
        {
            NodeType dataType = NodeType.Action;
            if (attr.Category.Contains("Composite")) dataType = NodeType.Composite;
            else if (attr.Category.Contains("Decorator")) dataType = NodeType.Decorator;
            else if (attr.Category.Contains("Condition")) dataType = NodeType.Condition;

            var nodeData = NodeData.Create(dataType, nodeType.Name, position);
            nodeData.nodeName = attr.DisplayName;
            nodeData.description = attr.Description;

            BTNodeView nodeView = dataType switch
            {
                NodeType.Composite => new CompositeNodeView(),
                NodeType.Decorator => new DecoratorNodeView(),
                NodeType.Condition => new ConditionNodeView(),
                _ => new ActionNodeView()
            };

            nodeView.Initialize(nodeData, this);
            return nodeView;
        }

        private void OnContextMenuPopulate(ContextualMenuPopulateEvent evt)
        {
            if (isRuntimeMode) return;

            var mousePos = evt.localMousePosition;

            // 组合节点
            evt.menu.AppendAction("创建节点/组合节点/Sequence", 
                _ => CreateNode<Runtime.Nodes.Composites.SequenceNode>(mousePos));
            evt.menu.AppendAction("创建节点/组合节点/Selector", 
                _ => CreateNode<Runtime.Nodes.Composites.SelectorNode>(mousePos));
            evt.menu.AppendAction("创建节点/组合节点/Parallel", 
                _ => CreateNode<Runtime.Nodes.Composites.ParallelNode>(mousePos));

            // 装饰节点
            evt.menu.AppendAction("创建节点/装饰节点/Repeater", 
                _ => CreateNode<Runtime.Nodes.Decorators.RepeaterNode>(mousePos));
            evt.menu.AppendAction("创建节点/装饰节点/Inverter", 
                _ => CreateNode<Runtime.Nodes.Decorators.InverterNode>(mousePos));
            evt.menu.AppendAction("创建节点/装饰节点/ConditionalAbort", 
                _ => CreateNode<Runtime.Nodes.Decorators.ConditionalAbortNode>(mousePos));

            // 动作节点
            evt.menu.AppendAction("创建节点/动作节点/Wait", 
                _ => CreateNode<Runtime.Nodes.Actions.WaitNode>(mousePos));
            evt.menu.AppendAction("创建节点/动作节点/Log", 
                _ => CreateNode<Runtime.Nodes.Actions.LogNode>(mousePos));

            // 条件节点
            evt.menu.AppendAction("创建节点/条件节点/CheckBlackboard", 
                _ => CreateNode<Runtime.Nodes.Conditions.CheckBlackboardNode>(mousePos));

            evt.menu.AppendSeparator();
            evt.menu.AppendAction("全选", _ => SelectAll());
            evt.menu.AppendAction("删除选中", _ => DeleteSelection());
        }

        /// <summary>
        /// 创建节点（不立即保存到asset）
        /// </summary>
        public void CreateNode<T>(Vector2 position) where T : BTNode
        {
            var type = typeof(T);
            if (nodeCreators.TryGetValue(type, out var creator))
            {
                var nodeView = creator(position);
                AddNodeView(nodeView, false); // 不立即保存到asset
                editorWindow.MarkUnsavedChanges();
            }
        }

        /// <summary>
        /// 添加节点视图
        /// </summary>
        public void AddNodeView(BTNodeView nodeView, bool fromAssetLoad = false)
        {
            if (nodeView == null || nodeView.NodeData == null) return;

            nodeViews[nodeView.NodeData.guid] = nodeView;
            AddElement(nodeView);

            // 只有在加载资产时才添加到asset数据，其他操作在保存时统一处理
            if (fromAssetLoad)
            {
                // 不操作asset，因为这是从asset加载的
            }
            else if (!isLoadingAsset)
            {
                // 标记有更改，但不立即更新asset
            }

            // 注册选中回调 - 修复节点选择问题
            nodeView.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 0 && !isRuntimeMode)
                {
                    editorWindow?.SetSelectedNode(nodeView);
                }
            });

            // 注册位置变化回调
            nodeView.RegisterCallback<GeometryChangedEvent>(evt =>
            {
                if (!isLoadingAsset && !isRuntimeMode)
                {
                    nodeView.NodeData.position = nodeView.GetPosition().position;
                    editorWindow.MarkUnsavedChanges();
                }
            });
        }

        /// <summary>
        /// 删除节点视图（不立即保存到asset）
        /// </summary>
        public void RemoveNodeView(BTNodeView nodeView)
        {
            if (nodeView == null || nodeView.NodeData == null) return;

            nodeViews.Remove(nodeView.NodeData.guid);
            RemoveElement(nodeView);

            // 不立即从asset删除，在保存时统一处理
            editorWindow.MarkUnsavedChanges();
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (isRuntimeMode || isLoadingAsset) return change;

            // 处理元素移除
            if (change.elementsToRemove != null)
            {
                foreach (var element in change.elementsToRemove)
                {
                    if (element is BTNodeView nodeView)
                    {
                        RemoveNodeView(nodeView);
                    }
                    else if (element is Edge edge)
                    {
                        var parentView = edge.output.node as BTNodeView;
                        var childView = edge.input.node as BTNodeView;
                        
                        if (parentView != null && childView != null)
                        {
                            // 不立即断开连接，在保存时统一处理
                            editorWindow.MarkUnsavedChanges();
                        }
                    }
                }
            }

            // 处理边创建
            if (change.edgesToCreate != null)
            {
                foreach (var edge in change.edgesToCreate)
                {
                    var parentView = edge.output.node as BTNodeView;
                    var childView = edge.input.node as BTNodeView;
                    
                    if (parentView != null && childView != null)
                    {
                        // 不立即连接，在保存时统一处理
                        editorWindow.MarkUnsavedChanges();
                    }
                }
            }

            // 处理节点移动
            if (change.movedElements != null)
            {
                foreach (var element in change.movedElements)
                {
                    if (element is BTNodeView nodeView)
                    {
                        nodeView.NodeData.position = nodeView.GetPosition().position;
                        if (!string.IsNullOrEmpty(nodeView.NodeData.parentGuid))
                        {
                            var parentNodeView = GetNodeByGuid(nodeView.NodeData.parentGuid) as BTNodeView;
                            if (parentNodeView != null)
                            {
                                UpdateChildNodeOrder(parentNodeView);
                            }
                        }
                        editorWindow.MarkUnsavedChanges();
                    }
                }
            }

            return change;
        }

        private void UpdateChildNodeOrder(BTNodeView parentNodeView)
        {
            if (parentNodeView?.NodeData == null) return;
            if (parentNodeView.NodeData.childrenGuids == null || 
                parentNodeView.NodeData.childrenGuids.Count <= 1) return;

            var childNodeViews = new List<BTNodeView>();
            foreach (var childGuid in parentNodeView.NodeData.childrenGuids)
            {
                var childView = GetNodeByGuid(childGuid) as BTNodeView;
                if (childView != null)
                {
                    childNodeViews.Add(childView);
                }
            }

            childNodeViews.Sort((a, b) => 
                a.GetPosition().x.CompareTo(b.GetPosition().x));

            var newChildGuids = childNodeViews
                .Select(n => n.NodeData.guid)
                .ToList();

            if (!parentNodeView.NodeData.childrenGuids.SequenceEqual(newChildGuids))
            {
                parentNodeView.NodeData.childrenGuids = newChildGuids;
                editorWindow.MarkUnsavedChanges();
            }
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList().Where(endPort =>
                endPort.direction != startPort.direction &&
                endPort.node != startPort.node &&
                endPort.portType == startPort.portType
            ).ToList();
        }

        /// <summary>
        /// 从资产加载
        /// </summary>
        public void LoadFromAsset(BehaviorTreeSO asset)
        {
            isLoadingAsset = true;
            currentAsset = asset;
            ClearGraph();

            if (asset?.treeData == null) 
            {
                isLoadingAsset = false;
                return;
            }

            // 创建所有节点视图
            foreach (var kvp in asset.treeData.allNodes)
            {
                var nodeData = kvp.Value;
                BTNodeView nodeView = nodeData.nodeType switch
                {
                    NodeType.Root => new RootNodeView(),
                    NodeType.Composite => new CompositeNodeView(),
                    NodeType.Decorator => new DecoratorNodeView(),
                    NodeType.Condition => new ConditionNodeView(),
                    _ => new ActionNodeView()
                };

                nodeView.Initialize(nodeData, this);
                nodeViews[nodeData.guid] = nodeView;
                AddElement(nodeView);
                
                // 注册选中回调
                nodeView.RegisterCallback<MouseDownEvent>(evt =>
                {
                    if (evt.button == 0 && !isRuntimeMode)
                    {
                        editorWindow?.SetSelectedNode(nodeView);
                    }
                });
            }

            // 创建连线
            foreach (var kvp in asset.treeData.allNodes)
            {
                var parentData = kvp.Value;
                if (!nodeViews.TryGetValue(kvp.Key, out var parentView)) continue;

                foreach (var childGuid in parentData.childrenGuids)
                {
                    if (!nodeViews.TryGetValue(childGuid, out var childView)) continue;

                    var edge = parentView.OutputPort?.ConnectTo(childView.InputPort);
                    if (edge != null)
                    {
                        AddElement(edge);
                    }
                }
            }

            isLoadingAsset = false;
        }

        /// <summary>
        /// 保存到资产（一次性更新所有更改）
        /// </summary>
        public void SaveToAsset()
        {
            if (currentAsset == null) return;

            // 清空现有数据
            currentAsset.treeData.allNodes.Clear();
            currentAsset.treeData.rootNodeGuid = null;

            // 重建节点数据
            foreach (var kvp in nodeViews)
            {
                var nodeView = kvp.Value;
                nodeView.NodeData.position = nodeView.GetPosition().position;
                currentAsset.treeData.allNodes[nodeView.NodeData.guid] = nodeView.NodeData;
            }

            // 重建连接关系
            foreach (var edge in edges)
            {
                var parentView = edge.output.node as BTNodeView;
                var childView = edge.input.node as BTNodeView;
                
                if (parentView != null && childView != null)
                {
                    if (!parentView.NodeData.childrenGuids.Contains(childView.NodeData.guid))
                    {
                        parentView.NodeData.childrenGuids.Add(childView.NodeData.guid);
                    }
                    childView.NodeData.parentGuid = parentView.NodeData.guid;
                }
            }

            // 设置根节点
            foreach (var kvp in nodeViews)
            {
                if (kvp.Value is RootNodeView)
                {
                    currentAsset.treeData.rootNodeGuid = kvp.Key;
                    break;
                }
            }

            EditorUtility.SetDirty(currentAsset);
        }

        /// <summary>
        /// 清空视图
        /// </summary>
        public void ClearGraph()
        {
            foreach (var nodeView in nodeViews.Values.ToList())
            {
                RemoveElement(nodeView);
            }
            nodeViews.Clear();

            foreach (var edge in edges.ToList())
            {
                RemoveElement(edge);
            }

            editorWindow.MarkUnsavedChanges();
        }

        /// <summary>
        /// 设置Runtime模式
        /// </summary>
        public void SetRuntimeMode(bool runtime)
        {
            isRuntimeMode = runtime;

            foreach (var nodeView in nodeViews.Values)
            {
                nodeView.SetRuntimeMode(runtime);
            }
        }

        /// <summary>
        /// 更新节点高亮（Runtime模式）
        /// </summary>
        public void UpdateNodeHighlight(string guid, NodeState state)
        {
            if (!isRuntimeMode) return;

            if (nodeViews.TryGetValue(guid, out var nodeView))
            {
                nodeView.UpdateHighlight(state);
            }
        }

        #region 复制粘贴

        private string OnSerializeGraphElements(IEnumerable<GraphElement> elements)
        {
            var data = new List<NodeData>();
            foreach (var element in elements)
            {
                if (element is BTNodeView nodeView)
                {
                    data.Add(nodeView.NodeData.Clone());
                }
            }
            return JsonUtility.ToJson(new SerializableList<NodeData> { items = data });
        }

        private void OnUnserializeAndPaste(string operationName, string data)
        {
            if (isRuntimeMode) return;

            try
            {
                var list = JsonUtility.FromJson<SerializableList<NodeData>>(data);
                if (list?.items == null) return;

                ClearSelection();

                var guidMap = new Dictionary<string, string>();
                foreach (var nodeData in list.items)
                {
                    var newGuid = Guid.NewGuid().ToString();
                    guidMap[nodeData.guid] = newGuid;
                    
                    nodeData.guid = newGuid;
                    nodeData.position += new Vector2(50, 50);

                    BTNodeView nodeView = nodeData.nodeType switch
                    {
                        NodeType.Composite => new CompositeNodeView(),
                        NodeType.Decorator => new DecoratorNodeView(),
                        NodeType.Condition => new ConditionNodeView(),
                        _ => new ActionNodeView()
                    };

                    nodeView.Initialize(nodeData, this);
                    AddNodeView(nodeView, false);
                    AddToSelection(nodeView);
                }

                foreach (var nodeData in list.items)
                {
                    for (int i = 0; i < nodeData.childrenGuids.Count; i++)
                    {
                        if (guidMap.TryGetValue(nodeData.childrenGuids[i], out var newGuid))
                        {
                            nodeData.childrenGuids[i] = newGuid;
                        }
                    }
                }

                editorWindow.MarkUnsavedChanges();
            }
            catch (Exception e)
            {
                Debug.LogError($"Paste failed: {e.Message}");
            }
        }

        [Serializable]
        private class SerializableList<T>
        {
            public List<T> items;
        }

        #endregion

        private void SelectAll()
        {
            ClearSelection();
            foreach (var nodeView in nodeViews.Values)
            {
                AddToSelection(nodeView);
            }
        }
    }
}