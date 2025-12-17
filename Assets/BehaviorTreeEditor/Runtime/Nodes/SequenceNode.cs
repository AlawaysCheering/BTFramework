using BehaviorTreeEditor.Runtime.Core;
using BehaviorTreeEditor.Runtime.Data;
using BehaviorTreeEditor.Runtime.Attributes;

namespace BehaviorTreeEditor.Runtime.Nodes.Composites
{
    /// <summary>
    /// 顺序节点 - 按顺序执行子节点，全部成功才返回成功
    /// </summary>
    [BTNode("Sequence", "Composites", "顺序执行所有子节点，全部成功才返回Success")]
    public class SequenceNode : BTNode
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

                // 通知行为树当前运行节点
                if (childState == NodeState.Running)
                {
                    Tree?.SetCurrentRunningNode(child);
                    return NodeState.Running;
                }

                // 子节点失败，整个序列失败
                if (childState == NodeState.Failure)
                {
                    return NodeState.Failure;
                }

                // 子节点成功，继续下一个
                currentChildIndex++;
            }

            // 所有子节点都成功
            return NodeState.Success;
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