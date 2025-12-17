// ===========================================
// 此文件由BTNodeGenerator自动生成
// 生成时间: 2025-12-17 16:47:31
// 请勿手动修改此文件
// ===========================================

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using BehaviorTreeEditor.Runtime.Data;
using BehaviorTreeEditor.Editor.Core;

namespace BehaviorTreeEditor.Editor.Nodes.Generated
{
    /// <summary>
    /// Jump 节点视图
    /// 跳跃
    /// </summary>
    public class JumpNodeView : ActionNodeView
    {
        public const string NodeTypeName = "JumpNode";
        public const string NodeDisplayName = "Jump";
        public const string NodeCategory = "Actions";

        protected override void CreateContent()
        {
            base.CreateContent();

            // 设置节点标题
            title = "Jump";


            // 添加节点描述
            var descLabel = new Label("跳跃");
            descLabel.style.fontSize = 10;
            descLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
            descLabel.style.whiteSpace = WhiteSpace.Normal;
            extensionContainer.Add(descLabel);
        }

        protected override Color GetNodeColor()
        {
            return new Color(0.2f, 0.5f, 0.2f);
        }
    }
}
