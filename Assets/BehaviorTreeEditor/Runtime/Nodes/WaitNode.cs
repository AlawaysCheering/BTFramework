using UnityEngine;
using BehaviorTreeEditor.Runtime.Core;
using BehaviorTreeEditor.Runtime.Data;
using BehaviorTreeEditor.Runtime.Attributes;
using Sirenix.OdinInspector;

namespace BehaviorTreeEditor.Runtime.Nodes.Actions
{
    /// <summary>
    /// 等待节点 - 等待指定时间
    /// </summary>
    [BTNode("Wait", "Actions", "等待指定秒数")]
    [GenerateNodeView]
    public class WaitNode : BTNode
    {
        [LabelText("等待时间(秒)")]
        [MinValue(0)]
        public float waitTime = 1f;

        [LabelText("使用黑板变量")]
        public bool useBlackboard = false;

        [LabelText("黑板变量名")]
        [ShowIf("useBlackboard")]
        public string blackboardKey;

        private float startTime;

        protected override void OnEnter()
        {
            startTime = Time.time;
            
            if (useBlackboard && !string.IsNullOrEmpty(blackboardKey))
            {
                waitTime = GetBlackboardValue<float>(blackboardKey);
            }
        }

        protected override NodeState OnEvaluate()
        {
            float elapsed = Time.time - startTime;
            
            if (elapsed >= waitTime)
            {
                return NodeState.Success;
            }

            // 通知行为树当前正在执行此节点
            Tree?.SetCurrentRunningNode(this);
            return NodeState.Running;
        }

        protected override void OnExit()
        {
            // 清理
        }
    }
}