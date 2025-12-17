using System;

namespace BehaviorTreeEditor.Runtime.Attributes
{
    /// <summary>
    /// 行为树节点标记特性 - 用于标识节点类并提供元数据
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class BTNodeAttribute : Attribute
    {
        /// <summary>
        /// 节点显示名称
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// 节点分类（用于编辑器右键菜单分组）
        /// </summary>
        public string Category { get; }

        /// <summary>
        /// 节点描述
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// 节点图标路径
        /// </summary>
        public string IconPath { get; set; }

        /// <summary>
        /// 节点颜色（十六进制）
        /// </summary>
        public string NodeColor { get; set; }

        public BTNodeAttribute(string displayName, string category, string description = "")
        {
            DisplayName = displayName;
            Category = category;
            Description = description;
        }
    }
}