using Framework.Core.Attribute;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.Common.BehaviourTree.Node
{
    public class RootNode : Node
    {
        [DisplayOnly]
        public Node child;

        public override string Description => "¸ù½Úµã";

        protected override void OnStart(object payload)
        {
        }

        protected override void OnStop(object payload)
        {
        }

        protected override void OnAbort(object payload)
        {
            child?.Abort(payload);
        }

        protected override void OnResume(object payload)
        {
        }

        protected override NodeState OnTick(float deltaTime, object payload)
        {
            return child?.Tick(deltaTime, payload) ?? NodeState.Success;
        }

        public override Node Clone()
        {
            var rootNode = Instantiate(this);
            rootNode.child =rootNode.child?.Clone();
            return rootNode;
        }

#if UNITY_EDITOR
        public override bool AddChildNode(Node child)
        {
            this.child = child;
            child.executeOrder = 1;
            return true;
        }
        public override bool RemoveChildNode(Node child)
        {
            if (child == this.child)
            {
                this.child=null;
                child.executeOrder = 1;
                return true;
            }
            return false;
        }
#endif
    }
}
    
