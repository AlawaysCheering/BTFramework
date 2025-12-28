namespace Framework.Common.BehaviourTree.Node.Decorator
{
    [NodeMenuItem("Decorator/Reverse")]
    public class ReverseNode : DecoratorNode
    {
        public override string Description => "返回相反结果的节点";

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            var state = child.Tick(deltaTime, payload);
            if (state == NodeState.Running)
            {
                return NodeState.Running;
            }

            return state == NodeState.Success ? NodeState.Failure : NodeState.Success;
        }
    }
}