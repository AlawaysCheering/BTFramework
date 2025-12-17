using BehaviorTreeEditor.Runtime.Core;
using BehaviorTreeEditor.Runtime.Data;
using BehaviorTreeEditor.Runtime.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BehaviorTreeEditor.Runtime.Nodes.Conditions
{
    /// <summary>
    /// 比较操作符
    /// </summary>
    public enum ComparisonOperator
    {
        Equal,          // ==
        NotEqual,       // !=
        GreaterThan,    // >
        LessThan,       // <
        GreaterOrEqual, // >=
        LessOrEqual     // <=
    }

    /// <summary>
    /// 检查黑板变量节点 - 条件判断
    /// </summary>
    [BTNode("CheckBlackboard", "Conditions", "检查黑板变量值")]
    [GenerateNodeView]
    public class CheckBlackboardNode : BTNode
    {
        [LabelText("变量名")]
        public string variableName;

        [LabelText("比较操作")]
        public ComparisonOperator comparison = ComparisonOperator.Equal;

        [LabelText("变量类型")]
        public BlackboardValueType valueType = BlackboardValueType.Bool;

        [LabelText("比较值(Int)")]
        [ShowIf("valueType", BlackboardValueType.Int)]
        public int compareIntValue;

        [LabelText("比较值(Float)")]
        [ShowIf("valueType", BlackboardValueType.Float)]
        public float compareFloatValue;

        [LabelText("比较值(Bool)")]
        [ShowIf("valueType", BlackboardValueType.Bool)]
        public bool compareBoolValue;

        [LabelText("比较值(String)")]
        [ShowIf("valueType", BlackboardValueType.String)]
        public string compareStringValue;

        protected override NodeState OnEvaluate()
        {
            if (string.IsNullOrEmpty(variableName) || Blackboard == null)
                return NodeState.Failure;

            var variable = Blackboard.GetVariable(variableName);
            if (variable == null)
                return NodeState.Failure;

            bool result = CompareValues(variable);
            return result ? NodeState.Success : NodeState.Failure;
        }

        private bool CompareValues(BlackboardVariable variable)
        {
            switch (valueType)
            {
                case BlackboardValueType.Bool:
                    return variable.boolValue == compareBoolValue;

                case BlackboardValueType.Int:
                    return CompareNumeric(variable.intValue, compareIntValue);

                case BlackboardValueType.Float:
                    return CompareNumeric(variable.floatValue, compareFloatValue);

                case BlackboardValueType.String:
                    return comparison == ComparisonOperator.Equal
                        ? variable.stringValue == compareStringValue
                        : variable.stringValue != compareStringValue;

                default:
                    return false;
            }
        }

        private bool CompareNumeric(float a, float b)
        {
            return comparison switch
            {
                ComparisonOperator.Equal => Mathf.Approximately(a, b),
                ComparisonOperator.NotEqual => !Mathf.Approximately(a, b),
                ComparisonOperator.GreaterThan => a > b,
                ComparisonOperator.LessThan => a < b,
                ComparisonOperator.GreaterOrEqual => a >= b,
                ComparisonOperator.LessOrEqual => a <= b,
                _ => false
            };
        }
    }
}