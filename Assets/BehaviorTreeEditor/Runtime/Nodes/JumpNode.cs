using System.Collections;
using System.Collections.Generic;
using BehaviorTreeEditor.Runtime.Attributes;
using BehaviorTreeEditor.Runtime.Core;
using BehaviorTreeEditor.Runtime.Data;
using UnityEngine;

namespace BehaviorTreeEditor.Runtime.Nodes
{
    [BTNode("Jump", "Actions", "跳跃")]
    [GenerateNodeView]
    public class JumpNode : BTNode
    {
        public float jumpForce = 0;
        protected override NodeState OnEvaluate()
        {
            Transform player = GetBlackboardValue<Transform>("player");
            if (player == null)
                return NodeState.Failure;

            Rigidbody rb = player.GetComponent<Rigidbody>();
            if(rb ==null) return NodeState.Failure;
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            return NodeState.Success;
        }
    }
}
