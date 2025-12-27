using System;

namespace Framework.Common.BehaviourTree.Node
{
    // AttributeUsage 指定这个特性可以应用在哪些目标上
    // AttributeTargets.Class 表示只能应用于类（class）上
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class NodeMenuItem : Attribute  // 继承自 Attribute 基类
    {
        // 存储节点在菜单中显示的名称
        public string ItemName;

        // 构造函数，接收菜单项名称作为参数
        public NodeMenuItem(string itemName)
        {
            ItemName = itemName;  // 将传入的名称赋值给 ItemName 字段
        }
    }
}