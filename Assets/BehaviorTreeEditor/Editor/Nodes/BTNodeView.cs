using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using BehaviorTreeEditor.Runtime.Data;
using System;

namespace BehaviorTreeEditor.Editor.Core
{
    /// <summary>
    /// 行为树节点视图基类
    /// </summary>
    public abstract class BTNodeView : Node
    {
        public NodeData NodeData { get; protected set; }
        public Port InputPort { get; protected set; }
        public Port OutputPort { get; protected set; }
        protected BehaviorTreeGraphView graphView;
        protected VisualElement stateIndicator;
        protected bool isRuntimeMode = false;

        // 节点状态颜色
        protected static readonly Color ColorInvalid = new Color(0.3f, 0.3f, 0.3f);
        protected static readonly Color ColorRunning = new Color(0.8f, 0.8f, 0.2f);
        protected static readonly Color ColorSuccess = new Color(0.2f, 0.8f, 0.2f);
        protected static readonly Color ColorFailure = new Color(0.8f, 0.2f, 0.2f);

        /// <summary>
        /// 初始化节点视图
        /// </summary>
        public virtual void Initialize(NodeData data, BehaviorTreeGraphView view)
        {
            NodeData = data;
            graphView = view;

            // 设置基本属性
            title = data.nodeName;
            viewDataKey = data.guid;

            // 设置位置
            SetPosition(new Rect(data.position, Vector2.zero));

            // 创建端口
            CreatePorts();

            // 创建状态指示器
            CreateStateIndicator();

            // 创建自定义内容
            CreateContent();

            // 刷新
            RefreshExpandedState();
            RefreshPorts();
        }

        /// <summary>
        /// 创建端口 - 子类可重写
        /// </summary>
        protected virtual void CreatePorts()
        {
            CreateInputPort();
            CreateOutputMutilPort();
        }
        protected virtual void CreateInputPort()
        {
            // 输入端口（接收父节点连接）
            InputPort = InstantiatePort(Orientation.Vertical, Direction.Input,
                Port.Capacity.Single, typeof(bool));
            InputPort.portName = "";
            InputPort.style.flexDirection = FlexDirection.Column;
            inputContainer.Add(InputPort);
        }
        protected virtual void CreateOutputMutilPort()
        {
            // 输出端口（连接子节点）
            OutputPort = InstantiatePort(Orientation.Vertical, Direction.Output, 
                Port.Capacity.Multi, typeof(bool));
            OutputPort.portName = "";
            OutputPort.style.flexDirection = FlexDirection.Column;
            outputContainer.Add(OutputPort);
        }
        protected virtual void CreateOutputSinglePort()
        {
            // 输出端口（连接子节点）
            OutputPort = InstantiatePort(Orientation.Vertical, Direction.Output,
                Port.Capacity.Single, typeof(bool));
            OutputPort.portName = "";
            OutputPort.style.flexDirection = FlexDirection.Column;
            outputContainer.Add(OutputPort);
        }
        /// <summary>
        /// 创建状态指示器
        /// </summary>
        protected virtual void CreateStateIndicator()
        {
            stateIndicator = new VisualElement();
            stateIndicator.name = "state-indicator";
            stateIndicator.style.position = Position.Absolute;
            stateIndicator.style.top = 0;
            stateIndicator.style.left = 0;
            stateIndicator.style.right = 0;
            stateIndicator.style.height = 4;
            stateIndicator.style.backgroundColor = ColorInvalid;
            
            mainContainer.Insert(0, stateIndicator);
        }

        /// <summary>
        /// 创建自定义内容 - 子类可重写
        /// </summary>
        protected virtual void CreateContent()
        {
            // 描述标签
            if (!string.IsNullOrEmpty(NodeData?.description))
            {
                var descLabel = new Label(NodeData.description);
                descLabel.style.fontSize = 10;
                descLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
                descLabel.style.whiteSpace = WhiteSpace.Normal;
                descLabel.style.maxWidth = 150;
                extensionContainer.Add(descLabel);
            }
        }

        /// <summary>
        /// 从数据更新视图
        /// </summary>
        public virtual void UpdateFromData()
        {
            if (NodeData == null) return;

            title = NodeData.nodeName;
            SetPosition(new Rect(NodeData.position, Vector2.zero));
        }

        /// <summary>
        /// 设置Runtime模式
        /// </summary>
        public virtual void SetRuntimeMode(bool runtime)
        {
            isRuntimeMode = runtime;
            
            // Runtime模式下禁用拖拽
            capabilities = runtime 
                ? capabilities & ~Capabilities.Movable & ~Capabilities.Deletable
                : capabilities | Capabilities.Movable | Capabilities.Deletable;
        }

        /// <summary>
        /// 更新高亮状态
        /// </summary>
        public virtual void UpdateHighlight(NodeState state)
        {
            if (stateIndicator == null) return;

            stateIndicator.style.backgroundColor = state switch
            {
                NodeState.Running => ColorRunning,
                NodeState.Success => ColorSuccess,
                NodeState.Failure => ColorFailure,
                _ => ColorInvalid
            };
        }

        /// <summary>
        /// 获取节点颜色 - 子类可重写
        /// </summary>
        protected virtual Color GetNodeColor()
        {
            return new Color(0.3f, 0.3f, 0.3f);
        }
        
    }
}