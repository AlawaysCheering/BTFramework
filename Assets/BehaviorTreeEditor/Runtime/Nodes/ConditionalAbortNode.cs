using BehaviorTreeEditor.Runtime.Core;
using BehaviorTreeEditor.Runtime.Data;
using BehaviorTreeEditor.Runtime.Attributes;
using Sirenix.OdinInspector;

namespace BehaviorTreeEditor.Runtime.Nodes.Decorators
{
    /// <summary>
    /// 条件打断节点 - 监控条件并在条件变化时打断
    /// </summary>
    [BTNode("ConditionalAbort", "Decorators", "监控条件变化，支持打断机制")]
    public class ConditionalAbortNode : BTNode
    {
        [LabelText("打断类型")]
        public AbortType abortType = AbortType.Self;

        [LabelText("监控的黑板变量")]
        public string watchedVariable;

        private object lastWatchedValue;
        private bool conditionMet = false;

        protected override void OnEnter()
        {
            // 记录初始值
            if (!string.IsNullOrEmpty(watchedVariable))
            {
                lastWatchedValue = Blackboard?.GetVariable(watchedVariable)?.GetValue();
            }
            conditionMet = EvaluateCondition();
        }

        protected override NodeState OnEvaluate()
        {
            if (Children.Count == 0)
                return NodeState.Failure;

            // 检查条件是否变化
            bool currentCondition = EvaluateCondition();
            
            if (currentCondition != conditionMet)
            {
                conditionMet = currentCondition;
                
                // 条件变化，触发打断逻辑
                if (ShouldAbort())
                {
                    // 打断当前子节点
                    foreach (var child in Children)
                    {
                        child.Abort();
                    }
                    
                    return conditionMet ? NodeState.Success : NodeState.Failure;
                }
            }

            // 正常执行子节点
            return Children[0].Evaluate();
        }

        /// <summary>
        /// 评估条件
        /// </summary>
        private bool EvaluateCondition()
        {
            if (string.IsNullOrEmpty(watchedVariable) || Blackboard == null)
                return true;

            var currentValue = Blackboard.GetVariable(watchedVariable)?.GetValue();
            
            // 简单的值变化检测
            bool changed = !Equals(currentValue, lastWatchedValue);
            lastWatchedValue = currentValue;
            
            // 这里可以根据具体需求实现更复杂的条件逻辑
            // 例如：检查bool变量是否为true
            if (currentValue is bool boolValue)
                return boolValue;
            
            // 检查数值是否大于0
            if (currentValue is int intValue)
                return intValue > 0;
            if (currentValue is float floatValue)
                return floatValue > 0;
            
            // 检查引用是否存在
            return currentValue != null;
        }

        /// <summary>
        /// 判断是否应该打断
        /// </summary>
        private bool ShouldAbort()
        {
            switch (abortType)
            {
                case AbortType.Self:
                    // 自我打断：当前子树正在执行时打断
                    return Children.Exists(c => c.State == NodeState.Running);
                    
                case AbortType.LowerPriority:
                    // 低优先级打断：需要配合BehaviorTree的打断检测
                    return true;
                    
                case AbortType.Both:
                    return true;
                    
                default:
                    return false;
            }
        }

        public override void Reset()
        {
            base.Reset();
            lastWatchedValue = null;
            conditionMet = false;
        }
    }
}