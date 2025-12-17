using BehaviorTreeEditor.Runtime.Core;
using BehaviorTreeEditor.Runtime.Attributes;
using Sirenix.OdinInspector;
using BehaviorTreeEditor.Runtime.Data;

namespace BehaviorTreeEditor.Runtime.Nodes.Decorators
{
    /// <summary>
    /// 重复节点 - 重复执行子节点指定次数
    /// </summary>
    [BTNode("Repeater", "Decorators", "重复执行子节点N次")]
    public class RepeaterNode : BTNode
    {
        [LabelText("重复次数")]
        [MinValue(1)]
        public int repeatCount = 1;

        [LabelText("无限循环")]
        public bool infinite = false;

        private int currentCount = 0;

        protected override void OnEnter()
        {
            currentCount = 0;
        }

        protected override NodeState OnEvaluate()
        {
            if (Children.Count == 0)
                return NodeState.Failure;

            var child = Children[0];

            while (infinite || currentCount < repeatCount)
            {
                var childState = child.Evaluate();

                if (childState == NodeState.Running)
                {
                    Tree?.SetCurrentRunningNode(child);
                    return NodeState.Running;
                }

                // 子节点完成一次
                currentCount++;
                child.Reset();

                // 非无限循环时检查是否完成
                if (!infinite && currentCount >= repeatCount)
                {
                    return NodeState.Success;
                }
            }

            return NodeState.Success;
        }

        protected override void OnExit()
        {
            currentCount = 0;
        }

        public override void Reset()
        {
            base.Reset();
            currentCount = 0;
        }
    }
}