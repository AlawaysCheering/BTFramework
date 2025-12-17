using System;

namespace BehaviorTreeEditor.Runtime.Attributes
{
    /// <summary>
    /// 自动生成NodeView标记特性
    /// 标记此特性的节点类将由代码生成器自动生成对应的编辑器NodeView类
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class GenerateNodeViewAttribute : Attribute
    {
        /// <summary>
        /// 自定义生成的NodeView类名（可选）
        /// </summary>
        public string CustomViewName { get; set; }

        /// <summary>
        /// 是否生成自定义Inspector
        /// </summary>
        public bool GenerateInspector { get; set; } = true;

        public GenerateNodeViewAttribute()
        {
        }

        public GenerateNodeViewAttribute(string customViewName)
        {
            CustomViewName = customViewName;
        }
    }
}