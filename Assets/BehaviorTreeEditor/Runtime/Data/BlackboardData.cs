using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace BehaviorTreeEditor.Runtime.Data
{
    /// <summary>
    /// 黑板变量类型
    /// </summary>
    public enum BlackboardValueType
    {
        Int,
        Float,
        Bool,
        String,
        Vector2,
        Vector3,
        GameObject,
        Transform,
        Object
    }

    /// <summary>
    /// 黑板变量数据
    /// </summary>
    [Serializable]
    public class BlackboardVariable
    {
        [LabelText("变量名")]
        public string name;

        [LabelText("类型")]
        public BlackboardValueType valueType;

        [LabelText("Int值")]
        [ShowIf("valueType", BlackboardValueType.Int)]
        public int intValue;

        [LabelText("Float值")]
        [ShowIf("valueType", BlackboardValueType.Float)]
        public float floatValue;

        [LabelText("Bool值")]
        [ShowIf("valueType", BlackboardValueType.Bool)]
        public bool boolValue;

        [LabelText("String值")]
        [ShowIf("valueType", BlackboardValueType.String)]
        public string stringValue;

        [LabelText("Vector2值")]
        [ShowIf("valueType", BlackboardValueType.Vector2)]
        public Vector2 vector2Value;

        [LabelText("Vector3值")]
        [ShowIf("valueType", BlackboardValueType.Vector3)]
        public Vector3 vector3Value;

        [LabelText("GameObject引用")]
        [ShowIf("valueType", BlackboardValueType.GameObject)]
        public GameObject gameObjectValue;

        [LabelText("Transform引用")]
        [ShowIf("valueType", BlackboardValueType.Transform)]
        public Transform transformValue;

        [LabelText("Object引用")]
        [ShowIf("valueType", BlackboardValueType.Object)]
        public UnityEngine.Object objectValue;

        /// <summary>
        /// 获取变量值（装箱）
        /// </summary>
        public object GetValue()
        {
            return valueType switch
            {
                BlackboardValueType.Int => intValue,
                BlackboardValueType.Float => floatValue,
                BlackboardValueType.Bool => boolValue,
                BlackboardValueType.String => stringValue,
                BlackboardValueType.Vector2 => vector2Value,
                BlackboardValueType.Vector3 => vector3Value,
                BlackboardValueType.GameObject => gameObjectValue,
                BlackboardValueType.Transform => transformValue,
                BlackboardValueType.Object => objectValue,
                _ => null
            };
        }

        /// <summary>
        /// 设置变量值
        /// </summary>
        public void SetValue(object value)
        {
            switch (valueType)
            {
                case BlackboardValueType.Int:
                    intValue = Convert.ToInt32(value);
                    break;
                case BlackboardValueType.Float:
                    floatValue = Convert.ToSingle(value);
                    break;
                case BlackboardValueType.Bool:
                    boolValue = Convert.ToBoolean(value);
                    break;
                case BlackboardValueType.String:
                    stringValue = value?.ToString() ?? string.Empty;
                    break;
                case BlackboardValueType.Vector2:
                    vector2Value = (Vector2)value;
                    break;
                case BlackboardValueType.Vector3:
                    vector3Value = (Vector3)value;
                    break;
                case BlackboardValueType.GameObject:
                    gameObjectValue = value as GameObject;
                    break;
                case BlackboardValueType.Transform:
                    transformValue = value as Transform;
                    break;
                case BlackboardValueType.Object:
                    objectValue = value as UnityEngine.Object;
                    break;
            }
        }

        /// <summary>
        /// 克隆变量
        /// </summary>
        public BlackboardVariable Clone()
        {
            return new BlackboardVariable
            {
                name = this.name,
                valueType = this.valueType,
                intValue = this.intValue,
                floatValue = this.floatValue,
                boolValue = this.boolValue,
                stringValue = this.stringValue,
                vector2Value = this.vector2Value,
                vector3Value = this.vector3Value,
                gameObjectValue = this.gameObjectValue,
                transformValue = this.transformValue,
                objectValue = this.objectValue
            };
        }
    }

    /// <summary>
    /// 黑板数据类 - 行为树的共享数据存储
    /// </summary>
    [Serializable]
    public class BlackboardData
    {
        [LabelText("黑板变量列表")]
        [ListDrawerSettings(ShowIndexLabels = true, Expanded = true)]
        public List<BlackboardVariable> variables = new List<BlackboardVariable>();

        // 变量变化事件，用于打断机制的条件重评估
        public event Action<string, object, object> OnVariableChanged;

        /// <summary>
        /// 获取变量
        /// </summary>
        public BlackboardVariable GetVariable(string name)
        {
            return variables.Find(v => v.name == name);
        }

        /// <summary>
        /// 获取泛型变量值
        /// </summary>
        public T GetValue<T>(string name)
        {
            var variable = GetVariable(name);
            if (variable == null) return default;
            
            var value = variable.GetValue();
            if (value is T typedValue)
                return typedValue;
            
            return default;
        }

        /// <summary>
        /// 设置变量值
        /// </summary>
        public void SetValue<T>(string name, T value)
        {
            var variable = GetVariable(name);
            if (variable == null)
            {
                Debug.LogWarning($"[Blackboard] Variable '{name}' not found");
                return;
            }

            var oldValue = variable.GetValue();
            variable.SetValue(value);
            
            // 触发变量变化事件
            OnVariableChanged?.Invoke(name, oldValue, value);
        }

        /// <summary>
        /// 添加变量
        /// </summary>
        public void AddVariable(string name, BlackboardValueType type)
        {
            if (GetVariable(name) != null)
            {
                Debug.LogWarning($"[Blackboard] Variable '{name}' already exists");
                return;
            }

            variables.Add(new BlackboardVariable
            {
                name = name,
                valueType = type
            });
        }

        /// <summary>
        /// 移除变量
        /// </summary>
        public void RemoveVariable(string name)
        {
            variables.RemoveAll(v => v.name == name);
        }

        /// <summary>
        /// 检查变量是否存在
        /// </summary>
        public bool HasVariable(string name)
        {
            return GetVariable(name) != null;
        }

        /// <summary>
        /// 清空所有变量
        /// </summary>
        public void Clear()
        {
            variables.Clear();
        }

        /// <summary>
        /// 深拷贝黑板数据
        /// </summary>
        public BlackboardData Clone()
        {
            var clone = new BlackboardData();
            foreach (var variable in variables)
            {
                clone.variables.Add(variable.Clone());
            }
            return clone;
        }
    }
}