using System.Collections.Generic;
using BehaviorTreeEditor.Runtime.Core;
using BehaviorTreeEditor.Runtime.Data;
using BehaviorTreeEditor.Runtime.Attributes;

namespace BehaviorTreeEditor.Runtime.Nodes.Composites
{
    /// <summary>
    /// 并行策略
    /// </summary>
    public enum ParallelPolicy
    {
        RequireAll,     // 所有成功才成功
        RequireOne      // 一个成功即成功
    }

    /// <summary>
    /// 并行节点 - 同时执行所有子节点
    /// </summary>
    [BTNode("Parallel", "Composites", "并行执行所有子节点")]
    public class ParallelNode : BTNode
    {
        public ParallelPolicy successPolicy = ParallelPolicy.RequireAll;
        public ParallelPolicy failurePolicy = ParallelPolicy.RequireOne;

        private List<NodeState> childrenStates = new List<NodeState>();

        protected override void OnEnter()
        {
            childrenStates.Clear();
            for (int i = 0; i < Children.Count; i++)
            {
                childrenStates.Add(NodeState.Running);
            }
        }

        protected override NodeState OnEvaluate()
        {
            if (Children.Count == 0)
                return NodeState.Success;

            int successCount = 0;
            int failureCount = 0;
            bool anyRunning = false;

            for (int i = 0; i < Children.Count; i++)
            {
                // 跳过已完成的节点
                if (childrenStates[i] != NodeState.Running)
                {
                    if (childrenStates[i] == NodeState.Success) successCount++;
                    else if (childrenStates[i] == NodeState.Failure) failureCount++;
                    continue;
                }

                var childState = Children[i].Evaluate();
                childrenStates[i] = childState;

                switch (childState)
                {
                    case NodeState.Success:
                        successCount++;
                        break;
                    case NodeState.Failure:
                        failureCount++;
                        break;
                    case NodeState.Running:
                        anyRunning = true;
                        break;
                }
            }

            // 检查成功条件
            if (successPolicy == ParallelPolicy.RequireAll && successCount == Children.Count)
                return NodeState.Success;
            if (successPolicy == ParallelPolicy.RequireOne && successCount > 0)
                return NodeState.Success;

            // 检查失败条件
            if (failurePolicy == ParallelPolicy.RequireAll && failureCount == Children.Count)
                return NodeState.Failure;
            if (failurePolicy == ParallelPolicy.RequireOne && failureCount > 0)
                return NodeState.Failure;

            // 还有节点在运行
            if (anyRunning)
                return NodeState.Running;

            return NodeState.Success;
        }

        protected override void OnExit()
        {
            childrenStates.Clear();
        }

        public override void Reset()
        {
            base.Reset();
            childrenStates.Clear();
        }
    }
}