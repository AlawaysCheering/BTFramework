using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Framework.Common.BehaviourTree.Node.Composite
{
    [NodeMenuItem("Composite/Sequence")]
    public class SequenceNode : CompositeNode
    {
        [SerializeField] private bool stateless = false;

        private int _lastTickIndex;
        public override string Description => "顺序执行子节点，进行逻辑与判断";

        protected override void OnStart(object payload)
        {
            _lastTickIndex = -1;
        }
        protected override NodeState OnTick(float deltaTime, object payload)
        {
            var index = stateless ? 0 : (_lastTickIndex<0 ? 0 : _lastTickIndex);
            if (index >= children.Count) return NodeState.Success;

            while(index<children.Count)
            {
                var child = children[index];
                var nodeState = child.Tick(deltaTime, payload);
                if(nodeState == NodeState.Running)
                {
                    if (stateless && index != _lastTickIndex && _lastTickIndex >= 0)
                    {
                        children[_lastTickIndex].Abort(payload);
                    }
                    _lastTickIndex = index;
                    return NodeState.Running;
                }
                switch (nodeState)
                {
                    case NodeState.Failure:
                        if(stateless&&index!=_lastTickIndex && _lastTickIndex >= 0)
                        {
                            children[_lastTickIndex].Abort(payload);
                        }
                        return NodeState.Failure;
                    case NodeState.Success:
                        index++;
                        break;
                }
            }
            return NodeState.Success;
        }
    }

}
