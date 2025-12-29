using Framework.Common.BehaviourTree.Node;
using Framework.Common.BehaviourTree.Node.Action;
using Framework.Common.BehaviourTree.Node.Composite;
using Framework.Common.BehaviourTree.Node.Decorator;
using Framework.DataStructure;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Plastic.Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Framework.Common.BehaviourTree.Editor.UI
{
    [Serializable]
    public class NodeCopyData
    {
        public List<NodeCopyItemData> nodes;
    }

    [Serializable]
    public class NodeCopyItemData
    {
        public string type;
        public SerializableVector3 position;
        public string comment;
        public bool stopWhenAbort;
    }
    /*
     负责处理
     1.可视化节点和连接

     2.处理用户交互（创建、删除、连接节点）

     3.实现复制粘贴功能

     4.同步数据模型与视图
     */
    public class BehaviourTreeGraphView : GraphView
    {
        public new class UXmlFactory : UxmlFactory<BehaviourTreeGraphView, GraphView.UxmlTraits>
        {
        }
        public System.Action<BehaviourTreeNodeView> OnNodeSelected;
        public System.Action<BehaviourTreeNodeView> OnNodeUnselected;
        public Vector2 MousePosition;

        private BehaviourTree _tree;

        private readonly List<Edge> _reconnectEdges = new();
        private readonly List<Edge> _disconnectEdges = new();

        public BehaviourTreeGraphView()
        {
            // 1. 添加网格背景
            var gridBackground = new GridBackground();
            gridBackground.name = "GridBackground";
            Insert(0, gridBackground);  // 插入到最底层

            // 2. 添加交互控制器
            this.AddManipulator(new ContentZoomer());    // 缩放
            this.AddManipulator(new ContentDragger());   // 拖动画布
            this.AddManipulator(new SelectionDragger()); // 拖动选中节点
            this.AddManipulator(new RectangleSelector());// 矩形选择

            // 3. 记录右键菜单时的鼠标位置
            RegisterCallback<ContextualMenuPopulateEvent>(_ =>
                MousePosition = Event.current.mousePosition
            );

            // 4. 加载样式表
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/Framework/Common/BTFramework/Editor/BehaviourTreeEditorWindow.uss");
            if(styleSheet != null )
            styleSheets.Add(styleSheet);

            // 注册撤销重做回调
            Undo.undoRedoPerformed += HandleRedoPerformed;

            //监听 剪切/复制/粘贴
            //序列化（复制）
            serializeGraphElements += (elements =>
            {
                var nodes = new List<NodeCopyItemData>();
                foreach(var graphElement in elements)
                {
                    if(graphElement is BehaviourTreeNodeView nodeView)
                    {
                        var nodeCopyData = new NodeCopyItemData
                        {
                            type = nodeView.Node.GetType().Name, // 节点类型
                            position = new SerializableVector3(nodeView.Node.position),
                            comment = nodeView.Node.comment,
                            stopWhenAbort = nodeView.Node.stopWhenAbort,
                        };
                        nodes.Add(nodeCopyData);
                    }
                }

                var data = new NodeCopyData()
                {
                    nodes = nodes
                };
                var nodeJson = JsonConvert.SerializeObject(data);// 序列化为JSON
                return nodeJson;// 复制到剪贴板
            });

            canPasteSerializedData += data => true;// 总是允许粘贴

            //反序列化（粘贴）
            unserializeAndPaste += (operationName, data) =>
            {
                var nodeCopyData = JsonConvert.DeserializeObject<NodeCopyData>(data);
                foreach(var node in nodeCopyData.nodes)
                {
                    //通过类型名称查找节点类型
                    Type nodeType = null;
                    var typeCollection = TypeCache.GetTypesDerivedFrom<Node.Node>();
                    foreach(var type in typeCollection)
                    {
                        if (type.Name.Equals(node.type))
                        {
                            nodeType = type;
                        }
                    }
                    if (nodeType == null) continue;

                    //创建新节点
                    var newNode = _tree?.CreateNode(nodeType);
                    if (newNode)
                    {
                        //创建节点视图
                        var nodeView = CreateNodeView(newNode, false);
                        //设置位置（稍微偏移避免重叠）
                        var newPosition = nodeView.GetPosition();
                        newPosition.x = node.position.x + 40;
                        newPosition.y = node.position.y + 40;
                        nodeView.SetPosition(newPosition);
                        //恢复节点属性
                        nodeView.Node.comment=node.comment;
                        nodeView.Node.stopWhenAbort = node.stopWhenAbort;
                        EditorUtility.SetDirty(nodeView.Node);
                    }
                }
            };
            //每100ms检查并处理需要重新连接或断开的边
            schedule.Execute(HandleEdgeReconnectedOrDisconnected).Every(100);
        }

        //右键菜单系统
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (_tree == null) return;
            // 1. 获取所有ActionNode派生类型
            var actionNodeTypes = TypeCache.GetTypesDerivedFrom<ActionNode>();
            AddMenuItem(actionNodeTypes);
            // 2. 获取所有DecoratorNode派生类型
            var decoratorNodeTypes = TypeCache.GetTypesDerivedFrom<DecoratorNode>();
            AddMenuItem(decoratorNodeTypes);
            // 3. 获取所有CompositeNode派生类型
            var compositeNodeTypes = TypeCache.GetTypesDerivedFrom<CompositeNode>();
            AddMenuItem(compositeNodeTypes);

            void AddMenuItem(TypeCache.TypeCollection types)
            {
                foreach(var type in types)
                {
                    if (type.IsAbstract) continue;// 跳过抽象类
                     // 检查是否有NodeMenuItem特性
                    if (type.IsDefined(typeof(NodeMenuItem), false))
                    {
                        var attributes = type.GetCustomAttributes(typeof(NodeMenuItem), false);
                        if (attributes.Length != 0)
                        {
                            var attribute = attributes[0] as NodeMenuItem;
                            evt.menu.AppendAction(attribute.ItemName, _ =>
                            {
                                // 创建新节点    
                                var node = _tree.CreateNode(type);
                                CreateNodeView(node, true);
                            });
                        }
                        else
                        {
                            // 默认菜单格式：基类名/类型名
                            evt.menu.AppendAction($"{type.BaseType.Name}/{type.Name}", _ =>
                            {
                                var node = _tree.CreateNode(type);
                                CreateNodeView(node, true);
                            });
                        }
                    }
                    else
                    {
                        evt.menu.AppendAction($"{type.BaseType.Name}/{type.Name}", _ =>
                        {
                            var node = _tree.CreateNode(type);
                            CreateNodeView(node, true);
                        });
                    }
                }
            }
        }
        //兼容端口检查
        public override List<Port> GetCompatiblePorts(Port startPort,NodeAdapter nodeAdapter)
        {
            // 这里是获取在某个端口连接时的满足条件的端口（满足条件是端口输入对输出、且端口对应的节点不同）
            return ports.ToList()
                           .Where(endPort => startPort.direction != endPort.direction && startPort.node != endPort.node).ToList();
        }

        // 重新加载视图
        internal void UpdateView(BehaviourTree tree)
        {
            _tree = tree;
            // 1. 清理现有元素
            graphViewChanged -= OnGraphViewChanged;
            DeleteElements(graphElements);
            if (!_tree) return;
            // 2. 重新注册变化回调
            graphViewChanged += OnGraphViewChanged;
            focusable = true;
            // 3. 确保有根节点和黑板
            if (!tree.rootNode)
            {
                tree.rootNode = tree.CreateRootNode();
            }

            if (!tree.blackboard)
            {
                tree.CreateBlackboard();
            }
            // 4. 创建所有节点视图
            tree.nodes.ForEach(node => CreateNodeView(node, false));
            // 5. 创建所有连接
            tree.nodes.ForEach(node =>
            {
                var parentNodeView = FindNodeView(node);
                tree.GetChildNodes(node).ForEach(childNode =>
                {
                    var childNodeView = FindNodeView(childNode);
                    var edge = parentNodeView.Output.ConnectTo(childNodeView.Input);
                    AddElement(edge);
                });
            });
            // 6. 排序所有节点视图
            SortAllNodeViews();
        }
        internal void UpdateNodeStates()
        {
            nodes.ForEach(node =>
            {
                if (node is BehaviourTreeNodeView treeNodeView)
                {
                    treeNodeView.UpdateState();
                }
            });
        }
        private BehaviourTreeNodeView FindNodeView(Node.Node node)
        {
            return GetNodeByGuid(node.guid) as BehaviourTreeNodeView;
        }
        //节点视图创建
        private BehaviourTreeNodeView CreateNodeView(Node.Node node, bool isNewNode)
        {
            var nodeView = new BehaviourTreeNodeView(node)
            {
                OnNodeSelected = OnNodeSelected,
                OnNodeUnselected=OnNodeUnselected,
            };
            AddElement(nodeView);
            if(isNewNode)
            {
                //用于右键菜单创建 在鼠标位置创建新节点
                var position = contentViewContainer.WorldToLocal(MousePosition);
                var newPostion = nodeView.GetPosition();
                newPostion.x=position.x;
                newPostion.y=position.y;
                nodeView.SetPosition(newPostion);
            }
            return nodeView;
        }
        private void SortAllNodeViews()
        {
            foreach (var node in nodes)
            {
                if (node is BehaviourTreeNodeView nodeView)
                {
                    nodeView.SortChildren();
                }
            }
        }

        //视图内容变化监听
        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            //处理元素删除
            HandleElementsRemoved();
            //处理连接创建
            HandleEdgesCreated();
            //处理元素移动
            HandleElementsMoved();
            return graphViewChange;
            void HandleElementsRemoved()
            {
                if (graphViewChange.elementsToRemove == null) return;
                foreach(var graphElement in graphViewChange.elementsToRemove)
                {
                    if(graphElement is BehaviourTreeNodeView nodeView)
                    {
                        _tree?.DeleteNode(nodeView.Node);
                    }

                    if(graphElement is Edge edge)
                    {
                        var parentNodeView = edge.output.node as BehaviourTreeNodeView;
                        var childNodeView = edge.input.node as BehaviourTreeNodeView;
                        if(!_tree|| !_tree.RemoveChildNode(parentNodeView!.Node, childNodeView!.Node))
                        {
                            _reconnectEdges.Add(edge);
                        }
                    }

                    SortAllNodeViews();
                }
            }
            void HandleEdgesCreated()
            {
                if(graphViewChange.edgesToCreate== null) return;

                foreach(var edge in graphViewChange.edgesToCreate)
                {
                    var parentNodeView = edge.output.node as BehaviourTreeNodeView;
                    var childNodeView = edge.input.node as BehaviourTreeNodeView;
                    if (!_tree || !_tree.AddChildNode(parentNodeView!.Node, childNodeView!.Node))
                    {
                        _disconnectEdges.Add(edge);
                    }
                }
                SortAllNodeViews();
            }
            void HandleElementsMoved()
            {
                if(graphViewChange.elementsToRemove== null) return;

                SortAllNodeViews();
            }
        }
        private void HandleRedoPerformed()
        {
            if (!_tree) return;
            UpdateView(_tree);
            AssetDatabase.SaveAssets();
        }

        private void HandleEdgeReconnectedOrDisconnected()
        {
            _reconnectEdges.ForEach(edge =>
            {
                edge.output?.Connect(edge);
                edge.input?.Connect(edge);
                AddElement(edge);
            });
            _reconnectEdges.Clear();
            _disconnectEdges.ForEach(edge =>
            {
                edge.output?.Disconnect(edge);
                edge.input?.Disconnect(edge);
                RemoveElement(edge);    
            });
            _disconnectEdges.Clear();
        }
    }
}