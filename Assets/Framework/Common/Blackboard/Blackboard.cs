using JetBrains.Annotations;
using NodeCanvas.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Framework.Common.Blackboard
{
    [CreateAssetMenu(menuName ="Blackboard/Blackboard")]
    public class Blackboard : ScriptableObject
    {
        [HideInInspector] public List<BlackboardVariable> parameters = new();
#if UNITY_EDITOR
        public void AddParameter(BlackboardVariable blackboardVariable)
        {   
            blackboardVariable.key=GetOnlyKey(blackboardVariable.key);
            parameters.Add(blackboardVariable);
            EditorUtility.SetDirty(this);

            string GetOnlyKey(string key)
            {
                var findIndex = parameters.FindIndex(variable=>variable.key==key);
                if(findIndex != -1)
                {
                    return GetOnlyKey(key + "_0");
                }
                return key;
            }
        }
        public void RemoveParameter(string key)
        {
            var findIndex = parameters.FindIndex(variable => variable.key == key);
            if(findIndex != -1)
            {
                parameters.RemoveAt(findIndex);
                EditorUtility.SetDirty(this);
            }
        }
#endif
        public void SetParameter(BlackboardVariable variable)
        {
            var findIndex = parameters.FindIndex(_variable => _variable.key==variable.key&&_variable.type==variable.type);
            if (findIndex == -1)
            {
                UnityEngine.Debug.LogWarning($"Variable {variable.key} is not existed");
                return;
            }
            switch (variable.type)
            {
                case BlackboardVariableType.Int:
                    parameters[findIndex].intValue = variable.intValue; break;
                case BlackboardVariableType.Float:
                    parameters[findIndex].floatValue = variable.floatValue; break;
                case BlackboardVariableType.Bool:
                    parameters[findIndex].boolValue = variable.boolValue; break;
                case BlackboardVariableType.String:
                    parameters[findIndex].stringValue = variable.stringValue;break;
            }
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        public void SetIntParameter(string key,int value)
        {
            var findIndex = parameters.FindIndex(_variable => _variable.key == key && _variable.type == BlackboardVariableType.Int);
            if (findIndex == -1)
            {
                UnityEngine.Debug.LogWarning($"Int variable {key} is not already existed");
                return;
            }
            parameters[findIndex].intValue = value;
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        public void SetFloatParameter(string key, float value)
        {
            var findIndex = parameters.FindIndex(variable =>
                variable.key == key && variable.type == BlackboardVariableType.Float);
            if (findIndex == -1)
            {
                UnityEngine.Debug.LogWarning($"Float variable {key} is not already existed");
                return;
            }

            parameters[findIndex].floatValue = value;
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        public void SetBoolParameter(string key, bool value)
        {
            var findIndex = parameters.FindIndex(variable =>
                variable.key == key && variable.type == BlackboardVariableType.Bool);
            if (findIndex == -1)
            {
                UnityEngine.Debug.LogWarning($"Boolean variable {key} is not already existed");
                return;
            }

            parameters[findIndex].boolValue = value;
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        public void SetStringParameter(string key, string value)
        {
            var findIndex = parameters.FindIndex(variable =>
                variable.key == key && variable.type == BlackboardVariableType.String);
            if (findIndex == -1)
            {
                UnityEngine.Debug.LogWarning($"String variable {key} is not already existed");
                return;
            }

            parameters[findIndex].stringValue = value;
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        public bool ContainsParameter(string key)
        {
            return parameters.Exists(variable => variable.key == key);
        }

        [CanBeNull]
        public BlackboardVariable GetParameter(string key)
        {
            var findInex = parameters.FindIndex(variable =>variable.key == key);
            if(findInex == -1)
            {
                UnityEngine.Debug.LogWarning($"Variable {key} is not already existed");
                return null;
            }
            return parameters[findInex];
        }
        public int GetIntParameter(string key)
        {
            var findIndex = parameters.FindIndex(variable => variable.key == key);
            if (findIndex == -1)
            {
                UnityEngine.Debug.LogWarning($"Int variable {key} is not already existed");
                return default;
            }

            return parameters[findIndex].intValue;
        }

        public float GetFloatParameter(string key)
        {
            var findIndex = parameters.FindIndex(variable => variable.key == key);
            if (findIndex == -1)
            {
                UnityEngine.Debug.LogWarning($"Float variable {key} is not already existed");
                return default;
            }

            return parameters[findIndex].floatValue;
        }

        public bool GetBoolParameter(string key)
        {
            var findIndex = parameters.FindIndex(variable => variable.key == key);
            if (findIndex == -1)
            {
                UnityEngine.Debug.LogWarning($"Boolean variable {key} is not already existed");
                return default;
            }

            return parameters[findIndex].boolValue;
        }

        public string GetStringParameter(string key)
        {
            var findIndex = parameters.FindIndex(variable => variable.key == key);
            if (findIndex == -1)
            {
                UnityEngine.Debug.LogWarning($"String variable {key} is not already existed");
                return default;
            }

            return parameters[findIndex].stringValue;
        }

        public void Synchronize(Blackboard blackboard)
        {
            foreach(var parameter in parameters)
            {
                if (blackboard.ContainsParameter(parameter.key))
                {
                    blackboard.SetParameter(parameter);
                }
            }
        }
    }
}

