using BehaviorTreeEditor.Runtime.Core;
using BehaviorTreeEditor.Runtime.Data;
using BehaviorTreeEditor.Runtime.Attributes;

namespace BehaviorTreeEditor.Runtime.Nodes.Composites
{
    /// <summary>
    /// 选择节点 - 按顺序执行子节点，任一成功即返回成功
    /// </summary>
    [BTNode("Selector", "Composites", "选择执行子节点，任一成功即返回Success")]
    public class SelectorNode : BTNode
    {
        private int currentChildIndex = 0;

        protected override void OnEnter()
        {
            currentChildIndex = 0;
        }

        protected override NodeState OnEvaluate()
        {
            if (Children.Count == 0)
                return NodeState.Failure;

            while (currentChildIndex < Children.Count)
            {
                var child = Children[currentChildIndex];
                var childState = child.Evaluate();

                // 正在执行
                if (childState == NodeState.Running)
                {
                    Tree?.SetCurrentRunningNode(child);
                    return NodeState.Running;
                }

                // 子节点成功，整个选择器成功
                if (childState == NodeState.Success)
                {
                    return NodeState.Success;
                }

                // 子节点失败，尝试下一个
                currentChildIndex++;
            }

            // 所有子节点都失败
            return NodeState.Failure;
        }

        protected override void OnExit()
        {
            currentChildIndex = 0;
        }

        public override void Reset()
        {
            base.Reset();
            currentChildIndex = 0;
        }
    }
}