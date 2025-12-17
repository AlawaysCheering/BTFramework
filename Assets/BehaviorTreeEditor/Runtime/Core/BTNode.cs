using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using BehaviorTreeEditor.Runtime.Data;

namespace BehaviorTreeEditor.Runtime.Core
{
    /// <summary>
    /// 行为树节点基类 - 所有节点的抽象基类
    /// </summary>
    public abstract class BTNode
    {
        // 节点数据引用
        public NodeData NodeData { get; protected set; }
        
        // 所属行为树
        public BehaviorTree Tree { get; set; }
        
        // 子节点列表
        public List<BTNode> Children { get; protected set; } = new List<BTNode>();
        
        // 父节点
        public BTNode Parent { get; set; }

        // 节点当前状态
        public NodeState State
        {
            get => NodeData?.nodeState ?? NodeState.Invalid;
            protected set
            {
                if (NodeData != null)
                    NodeData.nodeState = value;
            }
        }

        // 是否已启动
        protected bool isStarted = false;

        /// <summary>
        /// 初始化节点
        /// </summary>
        public virtual void Initialize(NodeData data, BehaviorTree tree)
        {
            NodeData = data;
            Tree = tree;
            State = NodeState.Invalid;
            isStarted = false;

            // 初始化字段值
            InitializeFieldsFromCustomData();
        }

        /// <summary>
        /// 从CustomData初始化字段值
        /// </summary>
        protected virtual void InitializeFieldsFromCustomData()
        {
            if (NodeData?.customData == null)
                return;

            Type nodeType = this.GetType();
            FieldInfo[] fields = nodeType.GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (FieldInfo field in fields)
            {
                // 跳过某些不需要的字段
                if (ShouldSkipFieldForInitialization(field))
                    continue;

                // 检查customData中是否有对应的变量
                if (NodeData.customData.HasVariable(field.Name))
                {
                    try
                    {
                        // 获取黑板中的值
                        object value = GetBlackboardValueForField(field);
                        
                        if (value != null)
                        {
                            // 设置字段值
                            field.SetValue(this, value);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[BTNode] Failed to initialize field '{field.Name}' for node {nodeType.Name}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 判断是否应该跳过字段初始化
        /// </summary>
        private bool ShouldSkipFieldForInitialization(FieldInfo field)
        {
            // 跳过只读字段
            if (field.IsInitOnly)
                return true;

            // 跳过某些特定名称的字段
            string[] skipFields = { "Children", "Parent", "Tree", "NodeData", "State", "isStarted" };
            if (Array.Exists(skipFields, name => string.Equals(name, field.Name, StringComparison.OrdinalIgnoreCase)))
                return true;

            // 跳过委托类型
            if (field.FieldType.IsSubclassOf(typeof(Delegate)))
                return true;

            return false;
        }

        /// <summary>
        /// 从黑板获取字段对应的值
        /// </summary>
        private object GetBlackboardValueForField(FieldInfo field)
        {
            Type fieldType = field.FieldType;
            
            // 处理可空类型
            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                fieldType = Nullable.GetUnderlyingType(fieldType);
            }

            // 使用反射调用泛型方法
            MethodInfo method = typeof(BlackboardData).GetMethod("GetValue");
            MethodInfo genericMethod = method.MakeGenericMethod(fieldType);
            
            return genericMethod.Invoke(NodeData.customData, new object[] { field.Name });
        }

        /// <summary>
        /// 将字段值同步回CustomData（用于运行时修改字段值后同步到数据）
        /// </summary>
        protected virtual void SyncFieldsToCustomData()
        {
            if (NodeData?.customData == null)
                return;

            Type nodeType = this.GetType();
            FieldInfo[] fields = nodeType.GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (FieldInfo field in fields)
            {
                if (ShouldSkipFieldForInitialization(field))
                    continue;

                if (NodeData.customData.HasVariable(field.Name))
                {
                    try
                    {
                        object currentValue = field.GetValue(this);
                        NodeData.customData.SetValue(field.Name, currentValue);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[BTNode] Failed to sync field '{field.Name}' to custom data: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 评估节点 - 核心执行方法
        /// </summary>
        public NodeState Evaluate()
        {
            // 首次执行时调用OnEnter
            if (!isStarted)
            {
                isStarted = true;
                OnEnter();
            }

            // 执行节点逻辑
            State = OnEvaluate();

            // 执行完成时调用OnExit
            if (State != NodeState.Running)
            {
                OnExit();
                isStarted = false;
            }

            return State;
        }

        /// <summary>
        /// 节点进入时调用
        /// </summary>
        protected virtual void OnEnter()
        {
            // 子类可重写
        }

        /// <summary>
        /// 节点评估逻辑 - 子类必须实现
        /// </summary>
        protected abstract NodeState OnEvaluate();

        /// <summary>
        /// 节点退出时调用
        /// </summary>
        protected virtual void OnExit()
        {
            // 子类可重写
        }

        /// <summary>
        /// 强制停止节点（被打断时调用）
        /// </summary>
        public virtual void Abort()
        {
            if (State == NodeState.Running)
            {
                OnStop();
                State = NodeState.Invalid;
                isStarted = false;

                // 递归停止所有子节点
                foreach (var child in Children)
                {
                    child.Abort();
                }
            }
        }

        /// <summary>
        /// 节点被打断时调用
        /// </summary>
        protected virtual void OnStop()
        {
            // 子类可重写，用于清理资源
        }

        /// <summary>
        /// 重置节点状态
        /// </summary>
        public virtual void Reset()
        {
            State = NodeState.Invalid;
            isStarted = false;
            foreach (var child in Children)
            {
                child.Reset();
            }
        }

        /// <summary>
        /// 添加子节点
        /// </summary>
        public virtual void AddChild(BTNode child)
        {
            if (child != null && !Children.Contains(child))
            {
                Children.Add(child);
                child.Parent = this;
            }
        }

        /// <summary>
        /// 移除子节点
        /// </summary>
        public virtual void RemoveChild(BTNode child)
        {
            if (child != null && Children.Contains(child))
            {
                Children.Remove(child);
                child.Parent = null;
            }
        }

        /// <summary>
        /// 获取黑板数据
        /// </summary>
        protected BlackboardData Blackboard => Tree?.Blackboard;

        /// <summary>
        /// 获取黑板变量值
        /// </summary>
        protected T GetBlackboardValue<T>(string key)
        {
            return Blackboard != null ? Blackboard.GetValue<T>(key) : default;
        }

        /// <summary>
        /// 设置黑板变量值
        /// </summary>
        protected void SetBlackboardValue<T>(string key, T value)
        {
            Blackboard?.SetValue(key, value);
        }
    }

    /// <summary>
    /// 根节点 - 行为树的入口
    /// </summary>
    public class RootNode : BTNode
    {
        protected override NodeState OnEvaluate()
        {
            if (Children.Count == 0)
                return NodeState.Failure;

            return Children[0].Evaluate();
        }
    }
}