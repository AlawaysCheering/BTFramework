using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.Common.BehaviourTree.Node.Composite
{
    [NodeMenuItem("Composite/Parallel Sequence")]
    public class ParallelSequenceNode : CompositeNode
    {
        private readonly HashSet<Node> _completeNodes = new();
        private bool _findFailure=false;

        public override string Description => "并行执行所有子节点，进行逻辑与判断";
        protected override void OnStart(object payload)
        {
            _completeNodes.Clear();
            _findFailure = false;
        }
        protected override NodeState OnTick(float deltaTime, object payload)
        {
            foreach (var child in children)
            {
                if (_completeNodes.Contains(child)) continue;
                var nodeState = child.Tick(deltaTime, payload);
                switch (nodeState)
                {
                    case NodeState.Success:
                        _completeNodes.Add(child);
                        break;
                    case NodeState.Failure:
                        _completeNodes.Add(child);
                        _findFailure = true; 
                        break;
                }
            }
            if(_findFailure)
            {
                foreach(var child in children)
                {
                    if(!_completeNodes.Contains(child)) continue;
                    child.Abort(payload);
                }
                return NodeState.Failure;
            }
            if(_completeNodes.Count >=children.Count) return NodeState.Success; 
            return NodeState.Running;
        }
    }
}
    
