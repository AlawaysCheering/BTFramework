using BehaviorTreeEditor.Runtime.Core;
using BehaviorTreeEditor.Runtime.Data;
using BehaviorTreeEditor.Runtime.Attributes;

namespace BehaviorTreeEditor.Runtime.Nodes.Decorators
{
    /// <summary>
    /// 反转节点 - 反转子节点的返回结果
    /// </summary>
    [BTNode("Inverter", "Decorators", "反转子节点结果：Success↔Failure")]
    public class InverterNode : BTNode
    {
        protected override NodeState OnEvaluate()
        {
            if (Children.Count == 0)
                return NodeState.Failure;

            var childState = Children[0].Evaluate();

            return childState switch
            {
                NodeState.Success => NodeState.Failure,
                NodeState.Failure => NodeState.Success,
                NodeState.Running => NodeState.Running,
                _ => NodeState.Invalid
            };
        }
    }
}