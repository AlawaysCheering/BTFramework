using UnityEngine;
namespace Framework.Common.BehaviourTree.Node.Composite
{
    [NodeMenuItem("Composite/Await All Failure")]
    public class AwaitAllFailureNode : CompositeNode
    {
        [SerializeField] private bool stateless = false;
        private int _lastTickIndex;
        public override string Description => "顺序执行所有子节点，直到所有子节点全部运行失败";
        protected override void OnStart(object payload)
        {
            _lastTickIndex = -1;
        }

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            var index = stateless ? 0 : (_lastTickIndex < 0 ? 0 : _lastTickIndex);
            if (index >= children.Count) return NodeState.Failure;

            while (index < children.Count)
            {
                var child = children[index];
                var nodeState = child.Tick(deltaTime, payload);
                if (nodeState == NodeState.Success || nodeState == NodeState.Running)
                {
                    if (stateless && index != _lastTickIndex && _lastTickIndex >= 0)
                    {
                        children[_lastTickIndex].Abort(payload);
                    }
                    _lastTickIndex = index;
                    return NodeState.Running;
                }
                if (nodeState == NodeState.Failure) ++index;
            }
            return NodeState.Failure;
        }
    }

}

