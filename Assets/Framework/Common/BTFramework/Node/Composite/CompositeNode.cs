using Framework.Core.Attribute;
using Sirenix.OdinValidator.Editor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Framework.Common.BehaviourTree.Node.Composite
{
    public class CompositeNode : Node
    {
        [DisplayOnly] public List<Node> children = new();

        public override string Description => "组合节点";

        public override Node Clone()
        {
            var compositeNode = Instantiate(this);
            foreach (var child in children)
            {
                compositeNode.children.Add(child.Clone());
            }
            return compositeNode;
        }

        protected override void OnAbort(object payload)
        {
            children.ForEach(child => child.Abort(payload));
        }
#if UNITY_EDITOR
        public override bool AddChildNode(Node child)
        {
            children.Add(child);
            child.executeOrder=children.Count;
            return true;
        }
        public override bool RemoveChildNode(Node child)
        {
            var remove = children.Remove(child);
            child.executeOrder = 1;
            for(var i = 0; i < children.Count; i++)
            {
                var childNode = children[i];
                childNode.executeOrder = i+1;
            }
            return remove;
        }
    }
#endif
}
