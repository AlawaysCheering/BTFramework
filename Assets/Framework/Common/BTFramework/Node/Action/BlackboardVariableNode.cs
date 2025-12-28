using Framework.Common.Blackboard;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.Common.BehaviourTree.Node.Action
{
    [NodeMenuItem("Action/Blackboard Variable")]
    public class BlackboardVariableNode : ActionNode,IBlackboardProvide
    {
        public Blackboard.Blackboard Blackboard=>blackboard;
        [SerializeField] private List<BlackboardVariable> variables;
        public override string Description => "黑板赋值节点，给黑板变量赋值";

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            foreach (var variable in variables)
            {
                blackboard.SetParameter(variable);
            }
            return NodeState.Success;
        }
    }

}
