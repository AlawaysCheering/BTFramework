using UnityEngine;
using UnityEditor.Experimental.GraphView;
using BehaviorTreeEditor.Runtime.Data;
using UnityEngine.UIElements;

namespace BehaviorTreeEditor.Editor.Core
{
    /// <summary>
    /// 根节点视图
    /// </summary>
    public class RootNodeView : BTNodeView
    {
        protected override void CreatePorts()
        {
            // 根节点只有输出端口
            CreateOutputMutilPort();
        }

        protected override Color GetNodeColor()
        {
            return new Color(0.4f, 0.2f, 0.2f);
        }
    }

    /// <summary>
    /// 组合节点视图
    /// </summary>
    public class CompositeNodeView : BTNodeView
    {

        protected override void CreateContent()
        {
            base.CreateContent();

            // 显示打断类型
            if (NodeData?.abortType != AbortType.None)
            {
                var abortLabel = new UnityEngine.UIElements.Label($"Abort: {NodeData.abortType}");
                abortLabel.style.fontSize = 9;
                abortLabel.style.color = new Color(0.8f, 0.6f, 0.2f);
                extensionContainer.Add(abortLabel);
            }
        }

        protected override Color GetNodeColor()
        {
            return new Color(0.2f, 0.4f, 0.6f);
        }
    }

    /// <summary>
    /// 装饰节点视图
    /// </summary>
    public class DecoratorNodeView : BTNodeView
    {
        protected override void CreatePorts()
        {
            CreateInputPort();
            CreateOutputSinglePort();
        }

        protected override Color GetNodeColor()
        {
            return new Color(0.4f, 0.4f, 0.2f);
        }
    }

    /// <summary>
    /// 动作节点视图
    /// </summary>
    public class ActionNodeView : BTNodeView
    {
        protected override void CreatePorts()
        {
            CreateInputPort();
        }

        protected override Color GetNodeColor()
        {
            return new Color(0.2f, 0.5f, 0.2f);
        }
    }

    /// <summary>
    /// 条件节点视图
    /// </summary>
    public class ConditionNodeView : BTNodeView
    {
        protected override void CreatePorts()
        {
            CreateInputPort();
        }

        protected override Color GetNodeColor()
        {
            return new Color(0.5f, 0.3f, 0.5f);
        }
    }
}