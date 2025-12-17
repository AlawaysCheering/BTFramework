using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Reflection;

namespace BehaviorTreeEditor.Runtime.Data
{
    /// <summary>
    /// 节点类型枚举
    /// </summary>
    public enum NodeType
    {
        Root,           // 根节点
        Composite,      // 组合节点
        Decorator,      // 装饰节点
        Action,         // 动作节点
        Condition       // 条件节点
    }

    /// <summary>
    /// 节点执行状态枚举
    /// </summary>
    public enum NodeState
    {
        Invalid,    // 无效/未执行
        Running,    // 执行中
        Success,    // 成功
        Failure     // 失败
    }

    /// <summary>
    /// 打断类型枚举 - 对标Behavior Designer的打断机制
    /// </summary>
    public enum AbortType
    {
        None,           // 无打断
        Self,           // 打断自身子树
        LowerPriority,  // 打断低优先级节点
        Both            // 两者都打断
    }

    /// <summary>
    /// 节点数据类 - 存储节点的序列化数据
    /// </summary>
    [Serializable]
    public class NodeData
    {
        [ReadOnly]
        [LabelText("节点GUID")]
        public string guid;

        [LabelText("节点类型")]
        [ReadOnly]
        public NodeType nodeType;

        [LabelText("节点名称")]
        public string nodeName;

        [LabelText("节点类名")]
        [ReadOnly]
        public string nodeClassName;

        [LabelText("节点Type")]
        [ReadOnly]
        public Type nodeClassType;

        [LabelText("编辑器位置")]
        [ReadOnly]
        public Vector2 position;

        [LabelText("子节点GUID列表")]
        [ReadOnly]
        public List<string> childrenGuids = new List<string>();

        [LabelText("父节点GUID")]
        [ReadOnly]
        public string parentGuid;

        [LabelText("当前状态")]
        [ReadOnly]
        public NodeState nodeState = NodeState.Invalid;

        [LabelText("打断类型")]
        [ShowIf("@nodeType == NodeType.Composite")]
        public AbortType abortType = AbortType.None;

        [LabelText("节点描述")]
        [TextArea(2, 4)]
        public string description;

        [LabelText("配置数据")]
        public BlackboardData customData = new BlackboardData();

         /// <summary>
        /// 根据节点类型自动初始化自定义数据
        /// </summary>
        public void InitCustomDataByNodeType()
        {
            if (string.IsNullOrEmpty(nodeClassName))
                return;

            customData.Clear();

            // 尝试获取节点类型
            Type nodeType = nodeClassType;
            if (nodeType == null)
            {
                Debug.LogWarning($"[NodeData] Cannot find node type: {nodeClassName}");
                return;
            }

            // 获取所有公共字段
            FieldInfo[] fields = nodeType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            
            foreach (FieldInfo field in fields)
            {
                // 跳过某些不需要的字段（如继承的字段或特殊字段）
                if (ShouldSkipField(field))
                    continue;

                // 将字段类型映射到黑板变量类型
                BlackboardValueType? valueType = MapFieldTypeToBlackboardType(field.FieldType);
                if (valueType.HasValue)
                {
                    // 添加变量到黑板数据
                    customData.AddVariable(field.Name, valueType.Value);
                    
                    // 设置默认值
                    SetDefaultValue(field.Name, field.FieldType);
                }
            }

            Debug.Log($"[NodeData] Initialized {customData.variables.Count} variables for node: {nodeClassName}");
        }
        /// <summary>
        /// 判断是否应该跳过该字段
        /// </summary>
        private bool ShouldSkipField(FieldInfo field)
        {
            // 跳过编译器生成的字段、只读字段、常量等
            if (field.IsInitOnly || field.IsLiteral)
                return true;

            // 跳过某些特定名称的字段
            string[] skipFields = { "children", "parent", "state", "guid", "position" };
            if (Array.Exists(skipFields, name => string.Equals(name, field.Name, StringComparison.OrdinalIgnoreCase)))
                return true;

            // 跳过某些特定类型的字段（如委托、事件等）
            if (field.FieldType.IsSubclassOf(typeof(Delegate)) || 
                field.FieldType.Name.Contains("EventHandler"))
                return true;

            return false;
        }
        /// <summary>
        /// 获取节点类型
        /// </summary>
        private Type GetNodeType()
        {
            // 尝试在当前程序集查找
            Type type = Type.GetType($"BehaviorTreeEditor.Runtime.Nodes.{nodeClassName}");
            if (type != null)
                return type;

            // 如果找不到，尝试在所有加载的程序集中查找
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(nodeClassName) ?? assembly.GetType($"BehaviorTreeEditor.Runtime.Nodes.{nodeClassName}");
                if (type != null)
                    return type;
            }

            return null;
        }
         /// <summary>
        /// 将字段类型映射到黑板变量类型
        /// </summary>
        private BlackboardValueType? MapFieldTypeToBlackboardType(Type fieldType)
        {
            if (fieldType == typeof(int)) return BlackboardValueType.Int;
            if (fieldType == typeof(float)) return BlackboardValueType.Float;
            if (fieldType == typeof(bool)) return BlackboardValueType.Bool;
            if (fieldType == typeof(string)) return BlackboardValueType.String;
            if (fieldType == typeof(Vector2)) return BlackboardValueType.Vector2;
            if (fieldType == typeof(Vector3)) return BlackboardValueType.Vector3;
            if (fieldType == typeof(GameObject)) return BlackboardValueType.GameObject;
            if (fieldType == typeof(Transform)) return BlackboardValueType.Transform;
            if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType)) return BlackboardValueType.Object;

            // 处理可空类型
            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                Type underlyingType = Nullable.GetUnderlyingType(fieldType);
                return MapFieldTypeToBlackboardType(underlyingType);
            }

            // 处理枚举类型（映射为int）
            if (fieldType.IsEnum)
                return BlackboardValueType.Int;

            Debug.LogWarning($"[NodeData] Unsupported field type: {fieldType.Name} for blackboard variable");
            return null;
        }

        /// <summary>
        /// 设置字段的默认值
        /// </summary>
        private void SetDefaultValue(string fieldName, Type fieldType)
        {
            object defaultValue = GetDefaultValueForType(fieldType);
            if (defaultValue != null)
            {
                // 使用反射调用泛型方法
                MethodInfo method = typeof(BlackboardData).GetMethod("SetValue");
                MethodInfo genericMethod = method.MakeGenericMethod(fieldType);
                genericMethod.Invoke(customData, new object[] { fieldName, defaultValue });
            }
        }

        /// <summary>
        /// 获取类型的默认值
        /// </summary>
        private object GetDefaultValueForType(Type type)
        {
            if (type == typeof(int)) return 0;
            if (type == typeof(float)) return 0f;
            if (type == typeof(bool)) return false;
            if (type == typeof(string)) return string.Empty;
            if (type == typeof(Vector2)) return Vector2.zero;
            if (type == typeof(Vector3)) return Vector3.zero;
            if (type.IsEnum) return Activator.CreateInstance(type); // 枚举的默认值

            // 对于引用类型，返回null
            if (!type.IsValueType)
                return null;

            // 对于其他值类型，返回默认实例
            return Activator.CreateInstance(type);
        }

        /// <summary>
        /// 创建新节点数据并自动初始化自定义数据
        /// </summary>
        public static NodeData Create(NodeType type,Type classType, Vector2 pos)
        {
            var nodeData = new NodeData
            {
                guid = Guid.NewGuid().ToString(),
                nodeType = type,
                nodeClassName = classType.Name,
                nodeClassType = classType,
                nodeName = classType.Name.Replace("Node", ""),
                position = pos,
                nodeState = NodeState.Invalid
            };

            // 自动初始化自定义数据
            nodeData.InitCustomDataByNodeType();

            return nodeData;
        }

        /// <summary>
        /// 深拷贝节点数据
        /// </summary>
        public NodeData Clone()
        {
            return new NodeData
            {
                guid = this.guid,
                nodeType = this.nodeType,
                nodeName = this.nodeName,
                nodeClassName = this.nodeClassName,
                position = this.position,
                childrenGuids = new List<string>(this.childrenGuids),
                parentGuid = this.parentGuid,
                nodeState = NodeState.Invalid,
                abortType = this.abortType,
                description = this.description,
                customData = this.customData.Clone()
            };
        }
    }
}