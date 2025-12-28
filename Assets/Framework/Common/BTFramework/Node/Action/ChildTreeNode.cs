using Framework.Common.Blackboard;
using Framework.Core.Attribute;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Framework.Common.BehaviourTree.Node.Action
{
    [NodeMenuItem("Action/ Child Tree")]
    public class ChildTreeNode : ActionNode
    {
        [SerializeField] private BehaviourTree tree;//预设
        [DisplayOnly, SerializeField] private BehaviourTree runtimeTree;//运行时

        public override string Description => "子树节点，同步父子树黑板数据，并运行子树流程";
        protected override void OnStart(object payload)
        {
            if(!runtimeTree)
            {
                runtimeTree = tree.Clone();
                runtimeTree.Parent = Tree;
            }
        }

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            blackboard.Synchronize(runtimeTree.blackboard);
            var state = runtimeTree.rootNode.Tick(deltaTime, payload);
            runtimeTree.rootNode.blackboard.Synchronize(blackboard);
            return state;
        }

        protected override void OnAbort(object payload)
        {
            runtimeTree.rootNode.Abort(payload);
        }
        protected override void OnStop(object payload)
        {
            base.OnStop(payload);
        }
    }

}


