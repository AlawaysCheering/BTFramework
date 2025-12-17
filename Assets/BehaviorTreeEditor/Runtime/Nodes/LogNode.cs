using UnityEngine;
using BehaviorTreeEditor.Runtime.Core;
using BehaviorTreeEditor.Runtime.Attributes;
using Sirenix.OdinInspector;
using BehaviorTreeEditor.Runtime.Data;

namespace BehaviorTreeEditor.Runtime.Nodes.Actions
{
    /// <summary>
    /// 日志节点 - 输出日志信息
    /// </summary>
    [BTNode("Log", "Actions", "输出日志信息到Console")]
    [GenerateNodeView]
    public class LogNode : BTNode
    {
        public enum LogType
        {
            Info,
            Warning,
            Error
        }

        [LabelText("日志类型")]
        public LogType logType = LogType.Info;

        [LabelText("日志内容")]
        [TextArea(2, 4)]
        public string message = "Log Message";

        protected override NodeState OnEvaluate()
        {
            string output = $"[BT] {message}";

            switch (logType)
            {
                case LogType.Info:
                    Debug.Log(output);
                    break;
                case LogType.Warning:
                    Debug.LogWarning(output);
                    break;
                case LogType.Error:
                    Debug.LogError(output);
                    break;
            }

            return NodeState.Success;
        }
    }
}